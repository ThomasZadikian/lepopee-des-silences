using Leds.Catalog.Application.PalaceLaws.Dtos;

namespace Leds.Catalog.Application.PalaceLaws.GetPalaceLawDefinitionByKey;

public sealed record GetPalaceLawDefinitionByKeyResponse(
    PalaceLawDefinitionDto? Definition);