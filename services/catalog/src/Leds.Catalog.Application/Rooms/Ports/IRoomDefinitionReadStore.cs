using Leds.Catalog.Application.Rooms.Dtos;

namespace Leds.Catalog.Application.Rooms.Ports;

public interface IRoomDefinitionReadStore
{
    Task<IReadOnlyCollection<RoomDefinitionDto>> ListActiveAsync(CancellationToken cancellationToken);
}