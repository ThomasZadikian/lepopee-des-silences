using Leds.Catalog.Domain.Enemies;

namespace Leds.Catalog.Application.Enemies.Definitions.Ports;

public interface IEnemyDefinitionReadStore
{
    Task<IReadOnlyCollection<IEnemyDefinition>> ListActiveAsync(
        CancellationToken cancellationToken);

    Task<IEnemyDefinition?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<IEnemyDefinition>> ListByRoomTypeAsync(
        string roomType,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<IEnemyDefinition>> ListCompatibleAsync(
        string roomType,
        int riskLevel,
        CancellationToken cancellationToken);
}
