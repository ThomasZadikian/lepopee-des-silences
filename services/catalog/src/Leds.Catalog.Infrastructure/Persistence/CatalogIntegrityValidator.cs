using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Domain.Enemies;
using Leds.Catalog.Domain.Gameplay;
using Leds.Catalog.Domain.Items;
using Leds.Catalog.Domain.Npcs;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.Persistence;

/// <summary>
/// Cross-definition publication gate. It validates persisted active content after
/// seeding/import so HTTP consumers can treat Catalog contracts as complete.
/// </summary>
public sealed class CatalogIntegrityValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CatalogDbContext _context;

    public CatalogIntegrityValidator(CatalogDbContext context) => _context = context;

    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var skills = await _context.SkillDefinitions
            .Where(skill => skill.Status == "Active")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        if (skills.Length == 0)
            errors.Add("Catalog must publish at least one active skill.");
        var skillKeys = skills.Select(skill => skill.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var characters = CharacterCombatDefinitionCatalog.All;
        var characterKeys = characters.Select(character => character.DefinitionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var characterArchetypes = characters.Select(character => character.CombatArchetypeCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (characters.Count == 0 || characterKeys.Count != characters.Count)
            errors.Add("Catalog character combat definitions must be non-empty and unique.");

        foreach (var character in characters)
        {
            Capture(errors, $"Character '{character.DefinitionKey}'", () =>
                EmotionalRegisterCatalog.Parse(
                    EmotionalRegisterCatalog.CodeOf(character.EmotionalRegister)));
            if (string.IsNullOrWhiteSpace(character.CombatArchetypeCode))
                errors.Add($"Character '{character.DefinitionKey}': combat archetype is required.");
        }

        foreach (var skill in skills)
        {
            Capture(errors, $"Skill '{skill.Key}'", () => EmotionalRegisterCatalog.Parse(skill.EmotionalRegister));
            if (skill.Category is not ("Physical" or "Magic"))
                errors.Add($"Skill '{skill.Key}': category must be Physical or Magic.");
            if (skill.TacticalAreaShape is not ("Single" or "Cross" or "Diamond" or "Map"))
                errors.Add($"Skill '{skill.Key}': tactical area shape is invalid.");
            if (skill.Audience is not ("Player" or "Enemy" or "Any"))
                errors.Add($"Skill '{skill.Key}': audience is invalid.");
            if (skill.ManaCost < 0 || skill.ChargeCost < 0 || skill.BasePower < 0 || skill.Cooldown < 0)
                errors.Add($"Skill '{skill.Key}': costs, power and cooldown must be non-negative.");

            var effects = Deserialize<Leds.Catalog.Domain.Skills.SkillEffectSpec>(
                errors, $"Skill '{skill.Key}' EffectsJson", skill.EffectsJson ?? "[]");
            Capture(errors, $"Skill '{skill.Key}'", () =>
                Leds.Catalog.Domain.Skills.SkillEffectSpecValidator.Validate(skill.Key, effects));

            var allowedArchetypes = Deserialize<string>(
                errors, $"Skill '{skill.Key}' AllowedArchetypesJson", skill.AllowedArchetypesJson ?? "[]");
            foreach (var unknownArchetype in allowedArchetypes.Where(archetype =>
                         !characterArchetypes.Contains(archetype)))
            {
                errors.Add($"Skill '{skill.Key}': archetype '{unknownArchetype}' has no Catalog character definition.");
            }
        }

        var enemies = await _context.EnemyDefinitions
            .Where(enemy => enemy.Status == "Active")
            .Include(enemy => enemy.StatBlock)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        if (enemies.Length == 0)
            errors.Add("Catalog must publish at least one active enemy.");

        foreach (var enemy in enemies)
        {
            Capture(errors, $"Enemy '{enemy.Key}'", () => EnemyArchetypeCatalog.Parse(enemy.Archetype));
            Capture(errors, $"Enemy '{enemy.Key}'", () => EmotionalRegisterCatalog.Parse(enemy.Registre ?? string.Empty));

            var declaredSkills = Deserialize<string>(errors, $"Enemy '{enemy.Key}' SkillKeysJson", enemy.SkillKeysJson);
            foreach (var missingSkill in declaredSkills.Where(key => !skillKeys.Contains(key)))
                errors.Add($"Enemy '{enemy.Key}': skill '{missingSkill}' does not exist or is not active.");

            if (enemy.StatBlock is null)
            {
                errors.Add($"Enemy '{enemy.Key}': stat block is required.");
            }
            else if (enemy.StatBlock.MaxVitality <= 0
                || enemy.StatBlock.Speed <= 0
                || enemy.StatBlock.Movement <= 0
                || enemy.StatBlock.AttackPower < 0
                || enemy.StatBlock.Defense < 0
                || enemy.StatBlock.StartingGuard < 0
                || enemy.StatBlock.Focus < 0
                || enemy.StatBlock.Mana < 0
                || enemy.StatBlock.Charge < 0
                || enemy.StatBlock.MagicAttack < 0
                || enemy.StatBlock.MagicDefense < 0)
            {
                errors.Add($"Enemy '{enemy.Key}': stat block contains invalid values.");
            }
            if (enemy.MenaceLevel is < 1 or > 10)
                errors.Add($"Enemy '{enemy.Key}': menace level must be between 1 and 10.");
        }

        var npcs = await _context.NpcDefinitions
            .Where(npc => npc.Status == "Active")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        if (npcs.Length == 0)
            errors.Add("Catalog must publish at least one active NPC.");
        foreach (var npc in npcs)
        {
            Capture(errors, $"NPC '{npc.Key}'", () =>
                EmotionalRegisterCatalog.Parse(npc.EmotionalAffinity));

            var offerings = Deserialize<NpcOffering>(
                errors, $"NPC '{npc.Key}' OfferingsJson", npc.OfferingsJson ?? "[]");
            foreach (var offering in offerings.Where(offering => offering.Kind == NpcOfferingKind.Companion))
            {
                if (string.IsNullOrWhiteSpace(offering.TargetKey) || offering.CompanionKit is null)
                {
                    errors.Add($"NPC '{npc.Key}': companion offering '{offering.Key}' requires a target and kit.");
                    continue;
                }

                if (!characterKeys.Contains(offering.TargetKey))
                    errors.Add($"NPC '{npc.Key}': companion target '{offering.TargetKey}' has no Catalog character definition.");

                var kit = offering.CompanionKit;
                if (kit.MaxVitality <= 0 || kit.Speed <= 0
                    || kit.AttackPower < 0 || kit.Defense < 0 || kit.StartingGuard < 0
                    || kit.Focus < 0 || kit.Mana < 0 || kit.Charge < 0
                    || kit.MagicAttack < 0 || kit.MagicDefense < 0)
                    errors.Add($"NPC '{npc.Key}': companion offering '{offering.Key}' has invalid stats.");

                foreach (var missingSkill in kit.SkillKeys.Where(key => !skillKeys.Contains(key)))
                    errors.Add($"NPC '{npc.Key}': companion skill '{missingSkill}' does not exist or is not active.");
            }
        }

        var items = await _context.ItemDefinitions
            .Where(item => item.Status == "Active")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        if (items.Length == 0)
            errors.Add("Catalog must publish at least one active item.");

        foreach (var item in items)
        {
            var effects = Deserialize<ItemEquipmentEffect>(
                errors, $"Item '{item.Key}' EquipmentEffectsJson", item.EquipmentEffectsJson ?? "[]");
            Capture(errors, $"Item '{item.Key}'", () => ItemEquipmentEffectValidator.Validate(item.Key, effects));

            foreach (var effect in effects.Where(effect => effect.Kind == ItemEquipmentEffectKind.GrantSkill))
            {
                if (!skillKeys.Contains(effect.SkillKey!))
                    errors.Add($"Item '{item.Key}': granted skill '{effect.SkillKey}' does not exist or is not active.");
            }
        }

        var matrix = EmotionalAffinityMatrix.Canonical;
        var registerCount = EmotionalRegisterCatalog.Active.Count;
        if (matrix.Rules.Count != registerCount * registerCount)
            errors.Add($"Emotional affinity matrix must contain {registerCount * registerCount} rules.");

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Catalog integrity validation failed:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(error => $"- {error}")));
        }
    }

    private static IReadOnlyCollection<T> Deserialize<T>(
        ICollection<string> errors,
        string field,
        string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
        }
        catch (JsonException exception)
        {
            errors.Add($"{field}: invalid JSON ({exception.Message}).");
            return [];
        }
    }

    private static void Capture(ICollection<string> errors, string source, Action validation)
    {
        try
        {
            validation();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            errors.Add($"{source}: {exception.Message}");
        }
    }
}
