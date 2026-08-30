using Leds.Catalog.Application.RoomBosses.Dtos;

namespace Leds.Catalog.Application.RoomBosses.ListActiveRoomBossDefinitions;

public sealed record ListActiveRoomBossDefinitionsResponse(
    IReadOnlyCollection<RoomBossDefinitionDto> Definitions);
