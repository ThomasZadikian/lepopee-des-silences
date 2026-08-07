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
    // Weapon/Accessory/Relic, or null when this category isn't equippable at all
    // (Consumable/Key/Currency/Material/...) — resolved server-side from Category via
    // CatalogRunItemMapper, the same authority the actual equip command uses, so the
    // frontend never re-derives it from raw category/type fields.
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
    int HimLitShardCost = 0);

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
