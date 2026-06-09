using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByKey;

public sealed record GetRoomBossDefinitionByKeyQuery(string Key)
    : IQuery<GetRoomBossDefinitionByKeyResponse>;
