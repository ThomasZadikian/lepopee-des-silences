namespace Leds.GameEngine.Application.Combats.EncounterDrafts;

public interface ICombatEncounterDraftGenerator
{
    Task<CombatEncounterDraft> GenerateAsync(
        CombatEncounterDraftContext context,
        CancellationToken cancellationToken = default);
}