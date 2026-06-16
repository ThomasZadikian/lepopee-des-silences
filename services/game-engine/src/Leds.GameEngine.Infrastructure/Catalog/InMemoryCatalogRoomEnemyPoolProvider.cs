using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;

namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class InMemoryCatalogRoomEnemyPoolProvider : ICatalogRoomEnemyPoolProvider
{
    private readonly Dictionary<string, CatalogRoomEnemyPoolSnapshot> _pools = new()
    {
        ["pool.threshold.default"] = new CatalogRoomEnemyPoolSnapshot(
            "pool.threshold.default", "1.0",
            [
                new CatalogRoomEnemyPoolEntrySnapshot("enemy-shadow-v1", 10, 0, 99, null, null),
                new CatalogRoomEnemyPoolEntrySnapshot("enemy-phantom-v1", 8, 1, 99, null, null),
                new CatalogRoomEnemyPoolEntrySnapshot("enemy-elite-guardian-v1", 3, 2, 99, "elite", null),
            ]),
    };

    public void Register(CatalogRoomEnemyPoolSnapshot pool)
    {
        _pools[pool.Key] = pool;
    }

    public Task<CatalogRoomEnemyPoolSnapshot?> GetEnemyPoolAsync(
        string enemyPoolKey,
        CancellationToken cancellationToken = default)
    {
        _pools.TryGetValue(enemyPoolKey, out var result);
        return Task.FromResult(result);
    }

    public Task<CatalogRoomEnemyPoolSnapshot?> GetEnemyPoolForRoomAsync(
        RoomEnemyPoolEligibilityContext context,
        CancellationToken cancellationToken = default)
    {
        var pool = _pools.Values.FirstOrDefault();
        return Task.FromResult(pool);
    }
}
