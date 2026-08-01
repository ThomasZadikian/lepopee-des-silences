namespace Leds.GameEngine.Application.Catalog;

public sealed record ItemDefinitionView(
    string Key,
    string DisplayName,
    string Description,
    string Category,
    string ItemType,
    string Rarity,
    string? EffectRunType,
    int EffectValue,
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
    int HimLitShardCost = 0);

public sealed record ItemEquipmentEffectView(
    string Kind,
    string? StatKind,
    int? Amount,
    string? SkillKey,
    string? AffinityRegister);

public sealed record ListActiveItemDefinitionsResponse(IReadOnlyCollection<ItemDefinitionView> Items);
