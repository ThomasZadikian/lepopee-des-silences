namespace Leds.GameEngine.Application.Catalog.Contracts;

/// <summary>
/// Runtime-oriented snapshot of an event template owned by the Catalog service.
/// </summary>
public sealed record EventTemplateSnapshot(
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string Type,
    string DefaultOutcomeKind,
    int MinRiskLevel,
    int MaxRiskLevel,
    bool RequiresPlayerChoice,
    IReadOnlyCollection<string> NarrativeTags);