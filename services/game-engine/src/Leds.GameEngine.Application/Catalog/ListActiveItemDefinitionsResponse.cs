namespace Leds.GameEngine.Application.Catalog;

public sealed record ItemDefinitionView(
    string Key,
    string DisplayName,
    string Description,
    string Category,
    string FlavorTag,
    string Rarity,
    string? EffectRunType,
    int EffectValue,
    // Transitional convenience sourced from AllowedSlots for older clients.
    string? EquipSlot = null,
    IReadOnlyCollection<string>? ReadablePages = null,
    IReadOnlyCollection<ItemEquipmentEffectView>? EquipmentEffects = null,
    bool IsContainer = false,
    int? ContainerCapacity = null,
    bool IsLiquid = false,
    int TacticalRange = 1,
    string TacticalAreaShape = "Single",
    bool RequiresLineOfSight = false,
    int? BasicAttackPower = null,
    string? BasicAttackCategory = null,
    int PalaceShardCost = 0,
    int HimLitShardCost = 0,
    IReadOnlyCollection<string>? AllowedSlots = null,
    string? UniqueEquipGroup = null,
    IReadOnlyCollection<string>? ProficiencyTags = null);

public sealed record ItemEquipmentEffectView(
    string Kind,
    string? StatKind,
    int? Amount,
    string? SkillKey,
    string? AffinityRegister,
    string? Condition = null,
    string? AffinityOutcome = null,
    int Priority = 0,
    int? DurationActivations = null,
    string? BehaviorCode = null);

public sealed record ListActiveItemDefinitionsResponse(IReadOnlyCollection<ItemDefinitionView> Items);
