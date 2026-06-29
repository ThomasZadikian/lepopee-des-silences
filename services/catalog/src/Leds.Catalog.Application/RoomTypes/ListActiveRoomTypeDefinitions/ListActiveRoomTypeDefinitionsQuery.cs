using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.RoomTypes.ListActiveRoomTypeDefinitions;

public sealed record ListActiveRoomTypeDefinitionsQuery()
    : IQuery<ListActiveRoomTypeDefinitionsResponse>;
