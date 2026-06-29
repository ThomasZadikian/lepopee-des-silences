using Leds.Catalog.Application.RoomTypes.Dtos;

namespace Leds.Catalog.Application.RoomTypes.Ports;

public interface IRoomTypeDefinitionReadStore
{
    Task<IReadOnlyCollection<RoomTypeDefinitionDto>> ListActiveAsync(
        CancellationToken cancellationToken);
}
