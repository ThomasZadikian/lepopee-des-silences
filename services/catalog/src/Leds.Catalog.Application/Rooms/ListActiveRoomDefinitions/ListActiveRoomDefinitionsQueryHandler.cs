using Leds.Catalog.Application.Rooms.Ports;
using MediatR;

namespace Leds.Catalog.Application.Rooms.ListActiveRoomDefinitions;

public sealed class ListActiveRoomDefinitionsQueryHandler
    : IRequestHandler<ListActiveRoomDefinitionsQuery, ListActiveRoomDefinitionsResponse>
{
    private readonly IRoomDefinitionReadStore _readStore;
    public ListActiveRoomDefinitionsQueryHandler(IRoomDefinitionReadStore readStore) => _readStore = readStore;

    public async Task<ListActiveRoomDefinitionsResponse> Handle(
        ListActiveRoomDefinitionsQuery request, CancellationToken cancellationToken)
        => new(await _readStore.ListActiveAsync(cancellationToken));
}