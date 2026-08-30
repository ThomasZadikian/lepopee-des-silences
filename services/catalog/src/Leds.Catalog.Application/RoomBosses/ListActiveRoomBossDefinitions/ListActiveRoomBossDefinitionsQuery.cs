using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.RoomBosses.ListActiveRoomBossDefinitions;

public sealed record ListActiveRoomBossDefinitionsQuery()
    : IQuery<ListActiveRoomBossDefinitionsResponse>;
