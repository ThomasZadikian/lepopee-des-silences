using Leds.Catalog.Application.Enemies.Definitions.Dtos;
using Leds.Catalog.Application.Enemies.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Enemies.Definitions.ListEnemyDefinitionsByRoomType;

public sealed class ListEnemyDefinitionsByRoomTypeQueryHandler
    : IRequestHandler<ListEnemyDefinitionsByRoomTypeQuery, ListEnemyDefinitionsByRoomTypeResponse>
{
    private readonly IEnemyDefinitionReadStore _readStore;

    public ListEnemyDefinitionsByRoomTypeQueryHandler(IEnemyDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListEnemyDefinitionsByRoomTypeResponse> Handle(
        ListEnemyDefinitionsByRoomTypeQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListByRoomTypeAsync(
            request.RoomType, cancellationToken);

        return new ListEnemyDefinitionsByRoomTypeResponse(
            definitions
                .Select(EnemyDefinitionDto.FromDomain)
                .ToArray());
    }
}
