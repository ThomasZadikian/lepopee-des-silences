namespace Leds.GameEngine.Application.Combats.EncounterDrafts;

public sealed record CombatEncounterDraftAlly(
    string AllyKey,
    string DisplayName,
    string Role,
    IReadOnlyCollection<string> Tags);