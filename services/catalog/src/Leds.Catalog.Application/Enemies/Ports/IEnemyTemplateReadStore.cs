using Leds.Catalog.Domain.Enemies;

namespace Leds.Catalog.Application.Enemies.Ports;

public interface IEnemyTemplateReadStore
{
    Task<IReadOnlyCollection<IEnemyTemplate>> ListActiveAsync(
        CancellationToken cancellationToken);

    Task<IEnemyTemplate?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}