using Leds.Catalog.Application.RoomThemeAffinities.Ports;
using MediatR;

namespace Leds.Catalog.Application.RoomThemeAffinities.ListRoomThemeAffinities;

public sealed class ListRoomThemeAffinitiesQueryHandler
    : IRequestHandler<ListRoomThemeAffinitiesQuery, ListRoomThemeAffinitiesResponse>
{
    private readonly IRoomThemeAffinityReadStore _readStore;
    public ListRoomThemeAffinitiesQueryHandler(IRoomThemeAffinityReadStore readStore) => _readStore = readStore;

    public async Task<ListRoomThemeAffinitiesResponse> Handle(
        ListRoomThemeAffinitiesQuery request, CancellationToken cancellationToken)
        => new(await _readStore.ListAllAsync(cancellationToken));
}
