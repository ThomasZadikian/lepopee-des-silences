using Leds.Catalog.Domain.RoomBosses;

namespace Leds.Catalog.Application.RoomBosses.Ports;

public interface IRoomBossDefinitionReadStore
{
    Task<IReadOnlyCollection<IRoomBossDefinition>> ListActiveAsync(
        CancellationToken cancellationToken);

    Task<IRoomBossDefinition?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken);

    Task<IRoomBossDefinition?> GetByRoomTypeAsync(
        string roomType,
        CancellationToken cancellationToken);
}
