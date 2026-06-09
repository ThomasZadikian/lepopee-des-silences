using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByRoomType;

public sealed record GetRoomBossDefinitionByRoomTypeQuery(string RoomType)
    : IQuery<GetRoomBossDefinitionByRoomTypeResponse>;
