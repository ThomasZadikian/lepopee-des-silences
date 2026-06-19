using Leds.Catalog.Application.Curses.Dtos;

namespace Leds.Catalog.Application.Curses.ListAvailableCurseDefinitions;

public sealed record ListAvailableCurseDefinitionsResponse(
    IReadOnlyCollection<CurseDefinitionDto> Definitions);
