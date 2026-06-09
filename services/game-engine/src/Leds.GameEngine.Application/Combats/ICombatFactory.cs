using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats;

public interface ICombatFactory
{
    Combat CreateFromDraft(CombatEncounterDraft draft);
}
