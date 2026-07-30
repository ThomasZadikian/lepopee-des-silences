using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Players.Ports;

namespace Leds.GameEngine.Application.Players;

/// <summary>
/// Shared skill-merge computation: for a given player-service character snapshot,
/// resolves the effective combat skill list (equipped skills, re-resolved against the
/// catalog for authoritative mechanics, falling back to the snapshot's own data when a
/// skill is missing from the catalog) plus any skill granted by an equipped item.
/// Extracted from <c>StartRunCommandHandler</c> so the Grimoire's mid-run skill resync
/// (<c>SyncPartySkillsCommandHandler</c>) can reuse the exact same merge logic instead
/// of duplicating it and silently drifting (e.g. dropping item-granted skills).
/// </summary>
public sealed class PlayerSkillMerger
{
    private readonly ICatalogContentGateway _catalogGateway;

    public PlayerSkillMerger(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task<IReadOnlyCollection<CatalogItemEquipmentEffect>> CollectEquippedItemEffectsAsync(
        IReadOnlyCollection<string> equippedItemKeys,
        CancellationToken cancellationToken)
    {
        if (equippedItemKeys.Count == 0)
        {
            return [];
        }

        var effects = new List<CatalogItemEquipmentEffect>();

        foreach (var itemKey in equippedItemKeys)
        {
            var result = await _catalogGateway.GetItemDefinitionByKeyAsync(itemKey, cancellationToken);
            if (result.IsSuccess && result.Value.EquipmentEffects is { Count: > 0 } itemEffects)
            {
                effects.AddRange(itemEffects);
            }
        }

        return effects;
    }

    public async Task<IReadOnlyCollection<CatalogItemDefinitionSnapshot>> ResolveEquippedItemsAsync(
        IReadOnlyCollection<string> equippedItemKeys,
        CancellationToken cancellationToken)
    {
        var definitions = new List<CatalogItemDefinitionSnapshot>();
        foreach (var itemKey in equippedItemKeys)
        {
            var result = await _catalogGateway.GetItemDefinitionByKeyAsync(
                itemKey, cancellationToken);
            if (result.IsSuccess)
                definitions.Add(result.Value);
        }

        return definitions;
    }

    /// <summary>
    /// mainCharacter.Skills comes from player-service's run-snapshot, which only
    /// guarantees the equipped skill KEY is correct — DisplayName/EffectType/BasePower
    /// there can be a best-effort guess (e.g. for a skill unlocked from an NPC offering
    /// like Mané's "Favorite de Elise"). Re-resolve each key against the catalog (the
    /// actual source of truth) and only fall back to the snapshot's own data if the
    /// catalog lookup misses, so a legendary/rare skill's real mechanics (EffectType,
    /// BasePower, Category, percent-of-max heal, ...) are what actually gets cast.
    /// </summary>
    public async Task<IReadOnlyCollection<MergedCharacterSkill>> MergeSkillsAsync(
        PlayerRunSnapshotCharacter character,
        IReadOnlyCollection<CatalogItemEquipmentEffect> equipmentEffects,
        CancellationToken cancellationToken,
        CatalogItemDefinitionSnapshot? equippedWeapon = null)
    {
        var grantedSkillKeys = equipmentEffects
            .Where(e => string.Equals(e.Kind, "GrantSkill", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(e.SkillKey))
            .Select(e => e.SkillKey!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(key => !character.Skills.Any(
                s => string.Equals(s.SkillDefinitionKey, key, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        var grantedSkills = await CollectGrantedSkillsAsync(grantedSkillKeys, cancellationToken);
        var catalogLearnedSkills = await CollectGrantedSkillsAsync(character.SkillKeys, cancellationToken);

        return character.Skills
            .Select(fallback =>
            {
                var fromCatalog = catalogLearnedSkills.FirstOrDefault(
                    s => string.Equals(s.Key, fallback.SkillDefinitionKey, StringComparison.OrdinalIgnoreCase));

                var merged = fromCatalog is not null
                    ? new MergedCharacterSkill(
                        fromCatalog.Key, fromCatalog.DisplayName, fromCatalog.SkillType, fromCatalog.TargetingType,
                        fromCatalog.EffectType, fromCatalog.ManaCost, fromCatalog.ChargeCost, fromCatalog.BasePower,
                        fromCatalog.Category, fromCatalog.BasePowerIsPercentOfMaxVitality,
                        fromCatalog.TacticalRange, fromCatalog.TacticalAreaShape,
                        fromCatalog.RequiresLineOfSight, fromCatalog.Cooldown,
                        fromCatalog.IsUltimate, fromCatalog.EmotionalRegister)
                    : new MergedCharacterSkill(
                        fallback.SkillDefinitionKey, fallback.DisplayName, fallback.SkillType, fallback.TargetingMode,
                        fallback.EffectType, fallback.ManaCost, fallback.ChargeCost, fallback.BasePower,
                        "Physical", false, 1, "Single", false, 0, false, "Neutral");

                return ApplyWeaponContract(merged, equippedWeapon);
            })
            .Concat(grantedSkills.Select(s => new MergedCharacterSkill(
                s.Key, s.DisplayName, s.SkillType, s.TargetingType, s.EffectType, s.ManaCost, s.ChargeCost,
                s.BasePower, s.Category, s.BasePowerIsPercentOfMaxVitality,
                s.TacticalRange, s.TacticalAreaShape, s.RequiresLineOfSight, s.Cooldown,
                s.IsUltimate, s.EmotionalRegister)))
            .ToArray();
    }

    private static MergedCharacterSkill ApplyWeaponContract(
        MergedCharacterSkill skill,
        CatalogItemDefinitionSnapshot? weapon)
    {
        if (!string.Equals(skill.Key, "skill.basic.strike", StringComparison.OrdinalIgnoreCase)
            || weapon is null
            || !string.Equals(weapon.ItemType, "Weapon", StringComparison.OrdinalIgnoreCase))
        {
            return skill;
        }

        return skill with
        {
            DisplayName = $"Attaque — {weapon.DisplayName}",
            BasePower = weapon.BasicAttackPower ?? 10,
            Category = string.IsNullOrWhiteSpace(weapon.BasicAttackCategory)
                ? "Physical"
                : weapon.BasicAttackCategory,
            TacticalRange = Math.Max(1, weapon.TacticalRange),
            TacticalAreaShape = string.IsNullOrWhiteSpace(weapon.TacticalAreaShape)
                ? "Single"
                : weapon.TacticalAreaShape,
            RequiresLineOfSight = weapon.RequiresLineOfSight
        };
    }

    private async Task<IReadOnlyCollection<CatalogSkillDefinition>> CollectGrantedSkillsAsync(
        IReadOnlyCollection<string> skillKeys,
        CancellationToken cancellationToken)
    {
        if (skillKeys.Count == 0)
        {
            return [];
        }

        var skills = new List<CatalogSkillDefinition>();

        foreach (var skillKey in skillKeys)
        {
            var skill = await _catalogGateway.GetSkillDefinitionByKeyAsync(skillKey, cancellationToken);
            if (skill is not null)
            {
                skills.Add(skill);
            }
        }

        return skills;
    }
}

/// <summary>Neutral skill shape produced by <see cref="PlayerSkillMerger"/>, converted by each
/// caller into its own domain type (<c>PlayerRuntimeSkill</c> or <c>RunCharacterSkillSnapshot</c>).</summary>
public sealed record MergedCharacterSkill(
    string Key,
    string DisplayName,
    string SkillType,
    string TargetingType,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower,
    string Category,
    bool BasePowerIsPercentOfMaxVitality,
    int TacticalRange,
    string TacticalAreaShape,
    bool RequiresLineOfSight,
    int Cooldown,
    bool IsUltimate,
    string EmotionalRegister);
