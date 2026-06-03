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
    IReadOnlyCollection<string> ImpactDomains);