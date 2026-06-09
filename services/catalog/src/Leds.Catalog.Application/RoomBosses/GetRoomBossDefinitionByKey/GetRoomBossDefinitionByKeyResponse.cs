using Leds.Catalog.Application.RoomBosses.Dtos;

namespace Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByKey;

public sealed record GetRoomBossDefinitionByKeyResponse(
    RoomBossDefinitionDto? Definition);
