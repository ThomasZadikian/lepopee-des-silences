using Leds.Catalog.Application.Rooms.Dtos;

namespace Leds.Catalog.Application.Rooms.ListActiveRoomDefinitions;

public sealed record ListActiveRoomDefinitionsResponse(IReadOnlyCollection<RoomDefinitionDto> Definitions);