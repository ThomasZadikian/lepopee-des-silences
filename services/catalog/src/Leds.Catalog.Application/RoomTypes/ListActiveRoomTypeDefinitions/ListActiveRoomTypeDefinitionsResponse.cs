using Leds.Catalog.Application.RoomTypes.Dtos;

namespace Leds.Catalog.Application.RoomTypes.ListActiveRoomTypeDefinitions;

public sealed record ListActiveRoomTypeDefinitionsResponse(
    IReadOnlyCollection<RoomTypeDefinitionDto> Definitions);
