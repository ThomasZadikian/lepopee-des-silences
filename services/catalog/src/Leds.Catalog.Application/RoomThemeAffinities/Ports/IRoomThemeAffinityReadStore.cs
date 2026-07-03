using Leds.Catalog.Application.RoomThemeAffinities.Dtos;

namespace Leds.Catalog.Application.RoomThemeAffinities.Ports;

public interface IRoomThemeAffinityReadStore
{
    Task<IReadOnlyCollection<RoomThemeAffinityDto>> ListAllAsync(CancellationToken cancellationToken);
}
