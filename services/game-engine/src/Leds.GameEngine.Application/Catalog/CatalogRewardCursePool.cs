namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogRewardCursePool(
    string Key,
    string DisplayName,
    string Description,
    string Version,
    IReadOnlyCollection<CatalogRewardCurseEntry> Entries);

public sealed record CatalogRewardCurseEntry(
    string Kind,
    string ResultKind,
    string? TargetKey,
    int Amount,
    IReadOnlyCollection<CatalogRewardCurseAvailability> Availability);

public sealed record CatalogRewardCurseAvailability(
    string Kind,
    int Value);