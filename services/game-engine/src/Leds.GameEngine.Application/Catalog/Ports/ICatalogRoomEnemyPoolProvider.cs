using Leds.GameEngine.Application.Catalog.Contracts;

namespace Leds.GameEngine.Application.Catalog.Ports;

public interface ICatalogRoomEnemyPoolProvider
{
    Task<CatalogRoomEnemyPoolSnapshot?> GetEnemyPoolAsync(
        string enemyPoolKey,
        CancellationToken cancellationToken = default);

    Task<CatalogRoomEnemyPoolSnapshot?> GetEnemyPoolForRoomAsync(
        RoomEnemyPoolEligibilityContext context,
        CancellationToken cancellationToken = default);
}
