using Leds.Catalog.Domain.Enemies.Loot;

namespace Leds.Catalog.Application.Enemies.Loot.Ports;

public interface IEnemyLootTableReadStore
{
    Task<IEnemyLootTable?> GetByEnemyKeyAsync(
        string enemyDefinitionKey,
        CancellationToken cancellationToken);
}
