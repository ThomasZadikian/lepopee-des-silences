using Leds.GameEngine.Application.Catalog.Contracts;

namespace Leds.GameEngine.Application.Catalog.Ports;

public interface ICatalogEnemyDefinitionProvider
{
    Task<CatalogEnemyDefinitionSnapshot?> GetEnemyByKeyAsync(
        string enemyDefinitionKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogEnemyDefinitionSnapshot>> ListEligibleEnemiesAsync(
        EnemyEligibilityContext context,
        CancellationToken cancellationToken = default);
}
