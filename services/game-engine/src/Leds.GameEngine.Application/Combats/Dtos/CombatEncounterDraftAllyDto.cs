using Leds.GameEngine.Application.Combats.EncounterDrafts;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatEncounterDraftAllyDto(
    string AllyKey,
    string DisplayName,
    string Role,
    IReadOnlyCollection<string> Tags)
{
    public static CombatEncounterDraftAllyDto FromDomain(CombatEncounterDraftAlly ally)
    {
        return new CombatEncounterDraftAllyDto(
            AllyKey: ally.AllyKey,
            DisplayName: ally.DisplayName,
            Role: ally.Role,
            Tags: ally.Tags);
    }
}
