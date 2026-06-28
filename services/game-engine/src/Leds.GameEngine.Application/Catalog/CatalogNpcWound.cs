namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcWound(
    string Key,
    string WoundRegister,
    string Reversibility,
    int TenseThreshold,
    int RuptureThreshold,
    IReadOnlyCollection<CatalogNpcTransgression> Transgressions,
    string? RupturedNarrativeKey);