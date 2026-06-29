using Leds.Catalog.Application.RoomTypes.Ports;
using MediatR;

namespace Leds.Catalog.Application.RoomTypes.ListActiveRoomTypeDefinitions;

public sealed class ListActiveRoomTypeDefinitionsQueryHandler
    : IRequestHandler<ListActiveRoomTypeDefinitionsQuery, ListActiveRoomTypeDefinitionsResponse>
{
    private readonly IRoomTypeDefinitionReadStore _readStore;

    public ListActiveRoomTypeDefinitionsQueryHandler(IRoomTypeDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveRoomTypeDefinitionsResponse> Handle(
        ListActiveRoomTypeDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListActiveAsync(cancellationToken);
        return new ListActiveRoomTypeDefinitionsResponse(definitions);
    }
}
