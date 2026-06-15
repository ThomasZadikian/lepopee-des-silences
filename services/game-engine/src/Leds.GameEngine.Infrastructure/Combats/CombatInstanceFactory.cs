using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Infrastructure.Combats;

public sealed class CombatInstanceFactory : ICombatInstanceFactory
{
    private const string PlayerTemplateKey = "player-runtime-v1";
    private const string PlayerDisplayName = "Élise";
    private const int PlayerMaxHealth = 40;
    private const int PlayerAttack = 12;
    private const int PlayerDefense = 6;
    private const int PlayerSpeed = 10;

    public CombatInstance CreateFromEnemyTemplate(EnemyTemplateSnapshot enemyTemplate)
    {
        var player = CombatantSnapshot.Create(
            PlayerTemplateKey,
            PlayerDisplayName,
            CombatantSide.Player,
            PlayerMaxHealth,
            PlayerAttack,
            PlayerDefense,
            PlayerSpeed);

        var enemy = CombatantSnapshot.Create(
            enemyTemplate.Key,
            enemyTemplate.Name,
            CombatantSide.Enemy,
            enemyTemplate.BaseHealth,
            enemyTemplate.BaseAttack,
            enemyTemplate.BaseDefense,
            enemyTemplate.BaseSpeed);

        return CombatInstance.Create(new[] { player, enemy });
    }
}