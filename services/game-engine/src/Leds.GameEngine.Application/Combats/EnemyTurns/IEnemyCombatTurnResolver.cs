using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.EnemyTurns;

public interface IEnemyCombatTurnResolver
{
    EnemyCombatTurnResolution Resolve(Combat combat);
}
