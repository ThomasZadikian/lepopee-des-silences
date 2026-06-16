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
    string? EffectSetKey);
