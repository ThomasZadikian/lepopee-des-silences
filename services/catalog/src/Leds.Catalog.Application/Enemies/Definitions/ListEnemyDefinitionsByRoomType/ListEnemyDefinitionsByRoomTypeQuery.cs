using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Enemies.Definitions.ListEnemyDefinitionsByRoomType;

public sealed record ListEnemyDefinitionsByRoomTypeQuery(string RoomType)
    : IQuery<ListEnemyDefinitionsByRoomTypeResponse>;
