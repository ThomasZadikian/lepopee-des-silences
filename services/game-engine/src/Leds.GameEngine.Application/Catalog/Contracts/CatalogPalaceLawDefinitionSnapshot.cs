namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogPalaceLawDefinitionSnapshot(
    string Key,
    string Version,
    string DisplayName,
    string Description,
    string? NarrativeText,
    string Scope,
    string Duration,
    string? Trigger,
    int Severity,
    string Visibility,
    string? EffectSetKey);
