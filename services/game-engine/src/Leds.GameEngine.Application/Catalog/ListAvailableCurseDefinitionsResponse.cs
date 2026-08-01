namespace Leds.GameEngine.Application.Catalog;

public sealed record CurseDefinitionView(
    string Key,
    string DisplayName,
    string Description,
    string? NarrativeText,
    int Severity,
    string Duration,
    string? Trigger);

public sealed record ListAvailableCurseDefinitionsResponse(IReadOnlyCollection<CurseDefinitionView> Curses);
