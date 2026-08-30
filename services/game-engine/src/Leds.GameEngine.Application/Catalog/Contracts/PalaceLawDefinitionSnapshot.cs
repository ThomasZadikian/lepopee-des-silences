namespace Leds.GameEngine.Application.Catalog.Contracts;

/// <summary>
/// Runtime-oriented snapshot of a palace law definition owned by the Catalog service.
/// </summary>
public sealed record PalaceLawDefinitionSnapshot(
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string Visibility,
    int Priority,
    IReadOnlyCollection<string> ImpactDomains,
    string? EffectSetKey = null,
    IReadOnlyCollection<CatalogEffectDefinitionSnapshot>? Effects = null,
    int BaseWeight = 1,
    int? MinDepth = null,
    int? MaxDepth = null,
    string Rarity = "Commun",
    string Polarity = "Neutre",
    bool IsMajeure = false,
    string? RoomKey = null,
    bool IsCumulExempt = false,
    IReadOnlyCollection<string>? ExclusionKeys = null);
