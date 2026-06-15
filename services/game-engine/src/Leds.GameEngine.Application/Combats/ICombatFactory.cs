using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats;

public interface ICombatFactory
{
    Combat CreateFromDraft(
        CombatEncounterDraft draft,
        PlayerRuntimeState? playerState = null,
        IReadOnlyCollection<RunModifier>? runModifiers = null);
}