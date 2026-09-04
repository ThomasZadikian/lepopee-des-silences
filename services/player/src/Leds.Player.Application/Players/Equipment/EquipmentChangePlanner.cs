using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Players;

namespace Leds.Player.Application.Players.Equipment;

public sealed class EquipmentChangePlanner
{
    public const int MaxTemporarySkills = 2;
    private readonly IEquipmentDefinitionGateway _catalog;
    private readonly IArchetypeDefinitionGateway _archetypes;
    public EquipmentChangePlanner(
        IEquipmentDefinitionGateway catalog,
        IArchetypeDefinitionGateway archetypes)
    {
        _catalog = catalog;
        _archetypes = archetypes;
    }

    public async Task<EquipmentChangePlan> PlanAsync(
        PlayerProfile profile,
        PlayerCharacterId characterId,
        OwnedItemInstanceId candidateId,
        EquipmentPosition targetPosition,
        int? currentVitality,
        int? currentMana,
        CancellationToken cancellationToken)
    {
        var character = profile.Roster.GetRequired(characterId);
        var candidate = profile.PermanentItems.FirstOrDefault(item => item.Id == candidateId);
        if (candidate is null)
            return EquipmentChangePlan.Blocked(targetPosition, candidateId, "ItemNotOwned", character.StatBlock);

        var candidateDefinition = await _catalog.GetByKeyAsync(candidate.ItemDefinitionKey, cancellationToken);
        if (candidateDefinition is null)
            return EquipmentChangePlan.Blocked(targetPosition, candidateId, "ItemDefinitionMissing", character.StatBlock);

        var blocking = new List<string>();
        if (!candidateDefinition.AllowedSlots.Any(slot => PositionAccepts(targetPosition, slot)))
            blocking.Add("SlotNotAllowed");

        if (candidateDefinition.ProficiencyTags.Count > 0)
        {
            var archetype = string.IsNullOrWhiteSpace(character.ArchetypeKey)
                ? null
                : await _archetypes.GetByKeyAsync(character.ArchetypeKey, cancellationToken);
            var proficiencies = archetype?.ProficiencyTags
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            if (candidateDefinition.ProficiencyTags.Any(required => !proficiencies.Contains(required)))
                blocking.Add("ProficiencyRequirementNotMet");
        }

        var currentlyEquipped = character.Items.FirstOrDefault(item => item.Position == targetPosition);
        var candidateAssignment = character.Items.FirstOrDefault(item => item.Id == candidateId && item.IsEquipped);
        if (candidateAssignment is not null && candidateAssignment.Position != targetPosition)
            blocking.Add("ItemAlreadyEquippedElsewhere");
        if (profile.Roster.Characters.Any(other => other.Id != characterId && other.Items.Any(item => item.Id == candidateId)))
            blocking.Add("ItemAlreadyEquippedElsewhere");

        var projected = character.Items
            .Where(item => item.IsEquipped && item.Id != currentlyEquipped?.Id)
            .Select(item => (item.Id, item.ItemDefinitionKey))
            .Append((candidate.Id, candidate.ItemDefinitionKey))
            .ToArray();
        if (projected.Select(item => item.Id).Distinct().Count() != projected.Length)
            blocking.Add("ItemAlreadyEquippedElsewhere");

        var currentDefinitions = await ResolveAsync(
            character.Items.Where(item => item.IsEquipped).Select(item => item.ItemDefinitionKey), cancellationToken);
        var projectedDefinitions = await ResolveAsync(projected.Select(item => item.ItemDefinitionKey), cancellationToken);
        if (currentDefinitions is null || projectedDefinitions is null)
            blocking.Add("ItemDefinitionMissing");

        var safeCurrent = currentDefinitions ?? [];
        var safeProjected = projectedDefinitions ?? [];
        var duplicateGroup = safeProjected
            .Where(item => !string.IsNullOrWhiteSpace(item.UniqueEquipGroup))
            .GroupBy(item => item.UniqueEquipGroup!, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateGroup is not null) blocking.Add("UniqueEquipGroupConflict");

        var permanentSkills = character.Skills.Where(skill => skill.IsEquipped)
            .Select(skill => skill.SkillDefinitionKey)
            .Append(PlayerCharacter.BasicSkillKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentTemporary = TemporarySkills(safeCurrent, permanentSkills);
        var projectedTemporary = TemporarySkills(safeProjected, permanentSkills);
        if (projectedTemporary.Count > MaxTemporarySkills)
            blocking.Add("TemporarySkillCapacityExceeded");

        var currentStats = Calculate(character.StatBlock, safeCurrent.SelectMany(item => item.EquipmentEffects));
        var projectedStats = Calculate(character.StatBlock, safeProjected.SelectMany(item => item.EquipmentEffects));
        var currentVitalityValue = Math.Min(currentVitality ?? currentStats.MaxVitality, currentStats.MaxVitality);
        var currentManaValue = Math.Min(currentMana ?? currentStats.Mana, currentStats.Mana);

        return new EquipmentChangePlan(
            targetPosition,
            new EquipmentItemPlan(candidate.Id.Value, candidate.ItemDefinitionKey, candidateDefinition.DisplayName),
            currentlyEquipped is null
                ? null
                : new EquipmentItemPlan(currentlyEquipped.Id.Value, currentlyEquipped.ItemDefinitionKey,
                    safeCurrent.FirstOrDefault(item => item.Key == currentlyEquipped.ItemDefinitionKey)?.DisplayName
                        ?? currentlyEquipped.ItemDefinitionKey),
            blocking.Count == 0,
            blocking.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            currentStats,
            projectedStats,
            EquipmentStatDelta.Create(currentStats, projectedStats),
            currentTemporary,
            projectedTemporary,
            projectedTemporary.Except(currentTemporary, StringComparer.OrdinalIgnoreCase).ToArray(),
            currentTemporary.Except(projectedTemporary, StringComparer.OrdinalIgnoreCase).ToArray(),
            currentVitalityValue,
            Math.Min(currentVitalityValue, projectedStats.MaxVitality),
            currentManaValue,
            Math.Min(currentManaValue, projectedStats.Mana),
            candidateDefinition.AllowedSlots,
            candidateDefinition.ProficiencyTags);
    }

    private async Task<IReadOnlyCollection<EquipmentDefinitionSnapshot>?> ResolveAsync(
        IEnumerable<string> keys, CancellationToken cancellationToken)
    {
        var definitions = new List<EquipmentDefinitionSnapshot>();
        foreach (var key in keys)
        {
            var definition = await _catalog.GetByKeyAsync(key, cancellationToken);
            if (definition is null) return null;
            definitions.Add(definition);
        }
        return definitions;
    }

    private static IReadOnlyCollection<string> TemporarySkills(
        IReadOnlyCollection<EquipmentDefinitionSnapshot> definitions,
        IReadOnlySet<string> permanentSkills) => definitions
        .SelectMany(item => item.EquipmentEffects)
        .Where(effect => string.Equals(effect.Kind, "GrantSkill", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(effect.SkillKey))
        .Select(effect => effect.SkillKey!)
        .Where(key => !permanentSkills.Contains(key))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static bool PositionAccepts(EquipmentPosition position, string slot) =>
        position is EquipmentPosition.Ring1 or EquipmentPosition.Ring2
            ? string.Equals(slot, "Ring", StringComparison.OrdinalIgnoreCase)
            : string.Equals(position.ToString(), slot, StringComparison.OrdinalIgnoreCase);

    private static EquipmentStats Calculate(
        PlayerCharacterStatBlock stats, IEnumerable<EquipmentEffectSnapshot> effects)
    {
        var effectArray = effects.ToArray();
        int Value(string key, int baseValue)
        {
            var flat = effectArray.Where(effect => effect.Kind.Equals("StatBonus", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(effect.StatKind, key, StringComparison.OrdinalIgnoreCase))
                .Sum(effect => effect.Amount ?? 0);
            var percent = effectArray.Where(effect => effect.Kind.Equals("StatBonusPercent", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(effect.StatKind, key, StringComparison.OrdinalIgnoreCase))
                .Sum(effect => effect.Amount ?? 0);
            return baseValue + flat + (int)Math.Round(baseValue * percent / 100d);
        }

        return new EquipmentStats(
            Math.Max(1, Value("MaxVitality", stats.MaxVitality)),
            Math.Max(0, Value("AttackPower", stats.AttackPower)),
            Math.Max(0, Value("MagicAttack", stats.MagicAttack)),
            Math.Max(0, Value("Defense", stats.Defense)),
            Math.Max(0, Value("MagicDefense", stats.MagicDefense)),
            Math.Max(0, Value("StartingGuard", stats.StartingGuard)),
            Math.Max(1, Value("Speed", stats.Speed)),
            Math.Max(0, Value("Initiative", stats.Initiative)),
            Math.Max(0, Value("Focus", stats.Focus)),
            Math.Max(0, Value("Mana", stats.Mana)),
            stats.Charge,
            Math.Max(1, Value("Movement", stats.Movement)));
    }
}

public sealed record EquipmentChangePlan(
    EquipmentPosition TargetPosition,
    EquipmentItemPlan CandidateItem,
    EquipmentItemPlan? CurrentlyEquippedItem,
    bool CanEquip,
    IReadOnlyCollection<string> BlockingReasons,
    EquipmentStats CurrentEffectiveStats,
    EquipmentStats ProjectedEffectiveStats,
    IReadOnlyCollection<EquipmentStatDelta> StatDeltas,
    IReadOnlyCollection<string> CurrentTemporarySkills,
    IReadOnlyCollection<string> ProjectedTemporarySkills,
    IReadOnlyCollection<string> GainedTemporarySkills,
    IReadOnlyCollection<string> LostTemporarySkills,
    int CurrentVitality,
    int ProjectedCurrentVitality,
    int CurrentMana,
    int ProjectedCurrentMana,
    IReadOnlyCollection<string> AllowedSlots,
    IReadOnlyCollection<string> ProficiencyTags)
{
    public static EquipmentChangePlan Blocked(
        EquipmentPosition position, OwnedItemInstanceId id, string reason, PlayerCharacterStatBlock stats)
    {
        var effective = EquipmentStats.From(stats);
        return new(position, new(id.Value, string.Empty, string.Empty), null, false, [reason],
            effective, effective, [], [], [], [], [], effective.MaxVitality, effective.MaxVitality,
            effective.Mana, effective.Mana, [], []);
    }
}

public sealed record EquipmentItemPlan(Guid ItemInstanceId, string DefinitionKey, string DisplayName);

public sealed record EquipmentStats(
    int MaxVitality, int AttackPower, int MagicAttack, int Defense, int MagicDefense,
    int StartingGuard, int Speed, int Initiative, int Focus, int Mana, int Charge, int Movement)
{
    public static EquipmentStats From(PlayerCharacterStatBlock stats) => new(
        stats.MaxVitality, stats.AttackPower, stats.MagicAttack, stats.Defense, stats.MagicDefense,
        stats.StartingGuard, stats.Speed, stats.Initiative, stats.Focus, stats.Mana, stats.Charge, stats.Movement);
}

public sealed record EquipmentStatDelta(string Stat, int Current, int Projected, int Delta)
{
    public static IReadOnlyCollection<EquipmentStatDelta> Create(EquipmentStats current, EquipmentStats projected) =>
    [
        new("MaxVitality", current.MaxVitality, projected.MaxVitality, projected.MaxVitality - current.MaxVitality),
        new("AttackPower", current.AttackPower, projected.AttackPower, projected.AttackPower - current.AttackPower),
        new("MagicAttack", current.MagicAttack, projected.MagicAttack, projected.MagicAttack - current.MagicAttack),
        new("Defense", current.Defense, projected.Defense, projected.Defense - current.Defense),
        new("MagicDefense", current.MagicDefense, projected.MagicDefense, projected.MagicDefense - current.MagicDefense),
        new("StartingGuard", current.StartingGuard, projected.StartingGuard, projected.StartingGuard - current.StartingGuard),
        new("Speed", current.Speed, projected.Speed, projected.Speed - current.Speed),
        new("Initiative", current.Initiative, projected.Initiative, projected.Initiative - current.Initiative),
        new("Focus", current.Focus, projected.Focus, projected.Focus - current.Focus),
        new("Mana", current.Mana, projected.Mana, projected.Mana - current.Mana),
        new("Movement", current.Movement, projected.Movement, projected.Movement - current.Movement)
    ];
}
