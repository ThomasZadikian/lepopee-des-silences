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
            // MenaceLevel is retained as historical authoring metadata only. Runtime danger is
            // expressed by RiskTier and encounter composition, so MENACE cannot gate publication.
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
            foreach (var offering in offerings.Where(offering => offering.Kind == NpcOfferingKind.StatPoint))
                errors.Add($"NPC '{npc.Key}': offering '{offering.Key}' uses retired permanent stat-point progression.");
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

            if (!string.IsNullOrWhiteSpace(npc.DialogueGraphJson))
            {
                NpcDialogueGraph? graph = null;
                try
                {
                    graph = JsonSerializer.Deserialize<NpcDialogueGraph>(npc.DialogueGraphJson, JsonOptions);
                }
                catch (JsonException exception)
                {
                    errors.Add($"NPC '{npc.Key}' DialogueGraphJson: invalid JSON ({exception.Message}).");
                }

                if (graph is not null)
                {
                    ValidateDialogueGraph(errors, npc.Key, graph);
                }
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

        var rooms = await _context.RoomDefinitions
            .Where(room => room.Status == "Active")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        var roomKeys = rooms.Select(room => room.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var bosses = await _context.RoomBossDefinitions
            .Where(boss => boss.Status == "Active")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        var bossKeys = bosses.Select(boss => boss.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var enemyKeys = enemies.Select(enemy => enemy.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var room in rooms)
        {
            if (string.IsNullOrWhiteSpace(room.Key) || string.IsNullOrWhiteSpace(room.Version)
                || string.IsNullOrWhiteSpace(room.Theme) || room.BaseWeight <= 0)
                errors.Add($"Room '{room.Key}': key, version, theme and positive weight are required.");
            if (room.MinDepth.HasValue && room.MaxDepth.HasValue && room.MinDepth > room.MaxDepth)
                errors.Add($"Room '{room.Key}': minimum depth cannot exceed maximum depth.");
            if (!string.IsNullOrWhiteSpace(room.BossDefinitionKey) && !bossKeys.Contains(room.BossDefinitionKey))
                errors.Add($"Room '{room.Key}': boss '{room.BossDefinitionKey}' does not exist or is not active.");
        }

        foreach (var boss in bosses)
        {
            if (string.IsNullOrWhiteSpace(boss.Version) || boss.BaseDifficulty <= 0)
                errors.Add($"Boss '{boss.Key}': version and positive base difficulty are required.");
            if (!string.IsNullOrWhiteSpace(boss.EnemyDefinitionKey)
                && !enemyKeys.Contains(boss.EnemyDefinitionKey))
                errors.Add($"Boss '{boss.Key}': enemy '{boss.EnemyDefinitionKey}' does not exist or is not active.");
        }

        var worlds = await _context.WorldDefinitions
            .Where(world => world.Status == "Active")
            .Include(world => world.EntryRoomDefinition)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        foreach (var world in worlds)
        {
            if (string.IsNullOrWhiteSpace(world.Key) || string.IsNullOrWhiteSpace(world.Version))
                errors.Add($"World '{world.Key}': key and version are required.");
            if (world.EntryRoomDefinition is null || !roomKeys.Contains(world.EntryRoomDefinition.Key))
                errors.Add($"World '{world.Key}': active entry room is required.");
        }

        var roomTypes = await _context.RoomTypeDefinitions
            .Where(type => type.Status == "Active")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        foreach (var roomType in roomTypes)
        {
            if (string.IsNullOrWhiteSpace(roomType.Key)
                || string.IsNullOrWhiteSpace(roomType.Version)
                || string.IsNullOrWhiteSpace(roomType.Theme))
                errors.Add($"Room type '{roomType.Key}': key, version and theme are required.");
        }

        var laws = await _context.PalaceLawDefinitions
            .Where(law => law.Status == "Active")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        var lawKeys = laws.Select(law => law.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var law in laws)
        {
            if (string.IsNullOrWhiteSpace(law.Version) || law.Severity <= 0 || law.BaseWeight <= 0)
                errors.Add($"Law '{law.Key}': version, positive severity and weight are required.");
            if (!string.IsNullOrWhiteSpace(law.RoomKey) && !roomKeys.Contains(law.RoomKey))
                errors.Add($"Law '{law.Key}': room '{law.RoomKey}' does not exist or is not active.");
            foreach (var missingLaw in Deserialize<string>(errors, $"Law '{law.Key}' ExclusionKeysJson", law.ExclusionKeysJson)
                         .Where(key => !lawKeys.Contains(key)))
                errors.Add($"Law '{law.Key}': excluded law '{missingLaw}' does not exist or is not active.");
        }

        var storySequences = await _context.StorySequenceDefinitions
            .Where(sequence => sequence.Status == "Active")
            .Include(sequence => sequence.Steps)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        foreach (var sequence in storySequences)
        {
            if (string.IsNullOrWhiteSpace(sequence.Key) || string.IsNullOrWhiteSpace(sequence.Version))
                errors.Add($"Story sequence '{sequence.Key}': key and version are required.");
            if (!sequence.Steps.Any(step => string.Equals(step.Key, sequence.EntryStepKey, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"Story sequence '{sequence.Key}': entry step '{sequence.EntryStepKey}' does not exist.");
            if (!sequence.Steps.Any(step => step.IsTerminal))
                errors.Add($"Story sequence '{sequence.Key}': at least one terminal step is required.");

            foreach (var step in sequence.Steps)
            {
                if (!string.IsNullOrWhiteSpace(step.RoomDefinitionKey) && !roomKeys.Contains(step.RoomDefinitionKey))
                    errors.Add($"Story step '{sequence.Key}/{step.Key}': room '{step.RoomDefinitionKey}' does not exist or is not active.");
                _ = Deserialize<JsonElement>(errors, $"Story step '{sequence.Key}/{step.Key}' ConditionsJson", step.ConditionsJson);
                _ = Deserialize<JsonElement>(errors, $"Story step '{sequence.Key}/{step.Key}' EffectsJson", step.EffectsJson);
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

    /// <summary>
    /// Structural checks on an already-parsed dialogue graph — SFD Système global de dialogues
    /// §9.4: duplicate choice id, a choice pointing at a node that doesn't exist, an entry node
    /// that doesn't exist. Strict-fail, same doctrine as the rest of this gate: a broken graph
    /// blocks publication rather than silently degrading at encounter time.
    /// </summary>
    private static void ValidateDialogueGraph(ICollection<string> errors, string npcKey, NpcDialogueGraph graph)
    {
        if (!graph.Nodes.ContainsKey(graph.EntryNodeKey))
        {
            errors.Add($"NPC '{npcKey}' dialogue graph: entry node '{graph.EntryNodeKey}' does not exist.");
        }

        foreach (var (nodeKey, node) in graph.Nodes)
        {
            if (!string.Equals(node.Key, nodeKey, StringComparison.Ordinal))
            {
                errors.Add($"NPC '{npcKey}' dialogue graph: node keyed '{nodeKey}' declares a mismatched Key '{node.Key}'.");
            }

            var choiceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var choice in node.Choices)
            {
                if (!choiceKeys.Add(choice.Key))
                {
                    errors.Add($"NPC '{npcKey}' dialogue graph: node '{nodeKey}' has a duplicate choice key '{choice.Key}'.");
                }

                if (choice.NextNodeKey is not null && !graph.Nodes.ContainsKey(choice.NextNodeKey))
                {
                    errors.Add(
                        $"NPC '{npcKey}' dialogue graph: choice '{choice.Key}' in node '{nodeKey}' " +
                        $"references missing node '{choice.NextNodeKey}'.");
                }

                foreach (var requirement in choice.Requirements)
                {
                    if (requirement.Kind == DialogueRequirementKind.WoundStateAtLeast
                        && (string.IsNullOrWhiteSpace(requirement.WoundKey) || requirement.RequiredWoundState is null))
                    {
                        errors.Add(
                            $"NPC '{npcKey}' dialogue graph: choice '{choice.Key}' in node '{nodeKey}' has a " +
                            "WoundStateAtLeast requirement missing its wound key or required state.");
                    }

                    if (requirement.Kind == DialogueRequirementKind.RelationshipScoreAtLeast
                        && requirement.RequiredRelationshipScore is null)
                    {
                        errors.Add(
                            $"NPC '{npcKey}' dialogue graph: choice '{choice.Key}' in node '{nodeKey}' has a " +
                            "RelationshipScoreAtLeast requirement missing its threshold.");
                    }
                }

                foreach (var consequence in choice.Consequences)
                {
                    if (consequence.Kind == ConsequenceKind.ArmWound || consequence.Kind == ConsequenceKind.SootheWound)
                    {
                        if (string.IsNullOrWhiteSpace(consequence.WoundKey))
                        {
                            errors.Add(
                                $"NPC '{npcKey}' dialogue graph: choice '{choice.Key}' in node '{nodeKey}' has a " +
                                $"{consequence.Kind} consequence missing its wound key.");
                        }
                    }

                    if (consequence.Kind == ConsequenceKind.GrantOffering && string.IsNullOrWhiteSpace(consequence.OfferingKey))
                    {
                        errors.Add(
                            $"NPC '{npcKey}' dialogue graph: choice '{choice.Key}' in node '{nodeKey}' has a " +
                            "GrantOffering consequence missing its offering key.");
                    }
                }
            }
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
