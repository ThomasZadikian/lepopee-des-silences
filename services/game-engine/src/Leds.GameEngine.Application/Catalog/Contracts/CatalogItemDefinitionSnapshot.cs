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
    IReadOnlyCollection<CatalogItemEquipmentEffect>? EquipmentEffects = null);

public sealed record CatalogItemEquipmentEffect(
    string Kind,
    string? StatKind,
    int? Amount,
    string? SkillKey,
    string? AffinityRegister);
