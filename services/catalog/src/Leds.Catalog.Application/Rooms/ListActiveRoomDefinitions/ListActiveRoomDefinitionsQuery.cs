using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Rooms.ListActiveRoomDefinitions;

public sealed record ListActiveRoomDefinitionsQuery() : IQuery<ListActiveRoomDefinitionsResponse>;