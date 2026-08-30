using Leds.Catalog.Application.RoomBosses.Dtos;
using Leds.Catalog.Application.RoomBosses.Ports;
using MediatR;

namespace Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByRoomType;

public sealed class GetRoomBossDefinitionByRoomTypeQueryHandler
    : IRequestHandler<GetRoomBossDefinitionByRoomTypeQuery, GetRoomBossDefinitionByRoomTypeResponse>
{
    private readonly IRoomBossDefinitionReadStore _readStore;

    public GetRoomBossDefinitionByRoomTypeQueryHandler(
        IRoomBossDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetRoomBossDefinitionByRoomTypeResponse> Handle(
        GetRoomBossDefinitionByRoomTypeQuery request,
        CancellationToken cancellationToken)
    {
        var definition = await _readStore.GetByRoomTypeAsync(
            request.RoomType,
            cancellationToken);

        return new GetRoomBossDefinitionByRoomTypeResponse(
            definition is null
                ? null
                : RoomBossDefinitionDto.FromDomain(definition));
    }
}
