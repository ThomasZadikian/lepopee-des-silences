using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Ports;

public interface ICombatInstanceFactory
{
    CombatInstance CreateFromEnemyTemplate(EnemyTemplateSnapshot enemyTemplate);
}