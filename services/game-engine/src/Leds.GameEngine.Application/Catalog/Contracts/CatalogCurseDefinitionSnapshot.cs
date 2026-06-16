namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogCurseDefinitionSnapshot(
    string Key,
    string Version,
    string DisplayName,
    string Description,
    string? NarrativeText,
    int Severity,
    string Duration,
    string? Trigger,
    string? EffectSetKey);
