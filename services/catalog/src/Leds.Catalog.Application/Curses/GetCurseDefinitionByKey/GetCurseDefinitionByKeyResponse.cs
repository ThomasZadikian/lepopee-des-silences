using Leds.Catalog.Application.Curses.Dtos;

namespace Leds.Catalog.Application.Curses.GetCurseDefinitionByKey;

public sealed record GetCurseDefinitionByKeyResponse(
    CurseDefinitionDto? Definition);
