namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogItemDefinitionSnapshot(
    string Key,
    string Version,
    string DisplayName,
    string Description,
    string? NarrativeText,
    string Category,
    string ItemType,
    string Rarity,
    string UsageMode,
    string Lifecycle,
    string StackPolicy,
    int MaxStack,
    bool IsUsableInCombat,
    bool IsUsableOutsideCombat,
    string? EffectSetKey,
    bool IsPermanentEligible = false,
    IReadOnlyCollection<CatalogItemEquipmentEffect>? EquipmentEffects = null,
    bool IsContainer = false,
    int? ContainerCapacity = null,
    bool IsLiquid = false,
    int EffectValue = 0,
    string? EffectRunType = null,
    IReadOnlyCollection<string>? ReadablePages = null,
    int TacticalRange = 1,
    string TacticalAreaShape = "Single",
    bool RequiresLineOfSight = false,
    int? BasicAttackPower = null,
    string? BasicAttackCategory = null,
    int PalaceShardCost = 0,
    int HimLitShardCost = 0);

public sealed record CatalogItemEquipmentEffect(
    string Kind,
    string? StatKind,
    int? Amount,
    string? SkillKey,
    string? AffinityRegister,
    // "key:value" (e.g. "room:Montagne", "weather:Accalmie") — null means always-on
    // while equipped. Re-evaluated fresh every combat (see CombatFactory), never
    // baked into PlayerStatMerger's static run-start stats.
    string? Condition = null,
    string? AffinityOutcome = null,
    int Priority = 0,
    int? DurationActivations = null,
    string? BehaviorCode = null,
    string? SourceDefinitionKey = null);
