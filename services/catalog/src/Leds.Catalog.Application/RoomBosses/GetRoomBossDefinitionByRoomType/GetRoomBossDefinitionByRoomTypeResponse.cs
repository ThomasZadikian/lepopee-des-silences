using Leds.Catalog.Application.RoomBosses.Dtos;

namespace Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByRoomType;

public sealed record GetRoomBossDefinitionByRoomTypeResponse(
    RoomBossDefinitionDto? Definition);
