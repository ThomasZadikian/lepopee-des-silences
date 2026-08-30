using Leds.Catalog.Application.RoomBosses.Dtos;
using Leds.Catalog.Application.RoomBosses.Ports;
using MediatR;

namespace Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByKey;

public sealed class GetRoomBossDefinitionByKeyQueryHandler
    : IRequestHandler<GetRoomBossDefinitionByKeyQuery, GetRoomBossDefinitionByKeyResponse>
{
    private readonly IRoomBossDefinitionReadStore _readStore;

    public GetRoomBossDefinitionByKeyQueryHandler(
        IRoomBossDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetRoomBossDefinitionByKeyResponse> Handle(
        GetRoomBossDefinitionByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var definition = await _readStore.GetByKeyAsync(
            request.Key,
            cancellationToken);

        return new GetRoomBossDefinitionByKeyResponse(
            definition is null
                ? null
                : RoomBossDefinitionDto.FromDomain(definition));
    }
}
