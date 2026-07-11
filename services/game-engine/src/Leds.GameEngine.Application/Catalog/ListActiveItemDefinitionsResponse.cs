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
    IReadOnlyCollection<string>? ReadablePages = null);

public sealed record ListActiveItemDefinitionsResponse(IReadOnlyCollection<ItemDefinitionView> Items);
