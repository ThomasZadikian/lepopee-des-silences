namespace Leds.GameEngine.Application.Catalog.Contracts;

/// <summary>
/// Runtime-oriented snapshot of an item template owned by the Catalog service.
/// </summary>
public sealed record ItemTemplateSnapshot(
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string ItemType,
    string Rarity,
    bool IsTemporary,
    IReadOnlyCollection<string> EffectTags);