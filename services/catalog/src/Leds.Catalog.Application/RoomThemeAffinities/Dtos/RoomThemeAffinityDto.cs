namespace Leds.Catalog.Application.RoomThemeAffinities.Dtos;

public sealed record RoomThemeAffinityDto(Guid Id, string ThemeFrom, string ThemeTo, decimal Weight);
