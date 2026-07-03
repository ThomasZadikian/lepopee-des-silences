using Leds.Catalog.Application.RoomThemeAffinities.Dtos;

namespace Leds.Catalog.Application.RoomThemeAffinities.ListRoomThemeAffinities;

public sealed record ListRoomThemeAffinitiesResponse(IReadOnlyCollection<RoomThemeAffinityDto> Affinities);
