using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.EnemyTurns;

public interface IEnemyCombatTurnResolver
{
    EnemyCombatTurnResolution Resolve(Combat combat);
    IReadOnlyCollection<CombatLogEntryDto> ResolveLeadingEnemyTurns(Combat combat);
}