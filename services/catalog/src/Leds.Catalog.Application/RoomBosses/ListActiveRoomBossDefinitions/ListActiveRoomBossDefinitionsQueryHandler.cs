using Leds.Catalog.Application.RoomBosses.Dtos;
using Leds.Catalog.Application.RoomBosses.Ports;
using MediatR;

namespace Leds.Catalog.Application.RoomBosses.ListActiveRoomBossDefinitions;

public sealed class ListActiveRoomBossDefinitionsQueryHandler
    : IRequestHandler<ListActiveRoomBossDefinitionsQuery, ListActiveRoomBossDefinitionsResponse>
{
    private readonly IRoomBossDefinitionReadStore _readStore;

    public ListActiveRoomBossDefinitionsQueryHandler(
        IRoomBossDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveRoomBossDefinitionsResponse> Handle(
        ListActiveRoomBossDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActiveRoomBossDefinitionsResponse(
            definitions
                .Select(RoomBossDefinitionDto.FromDomain)
                .ToArray());
    }
}
