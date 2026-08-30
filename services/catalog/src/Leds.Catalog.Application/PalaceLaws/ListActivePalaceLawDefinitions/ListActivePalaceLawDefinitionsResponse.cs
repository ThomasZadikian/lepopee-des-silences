using Leds.Catalog.Application.PalaceLaws.Dtos;

namespace Leds.Catalog.Application.PalaceLaws.ListActivePalaceLawDefinitions;

public sealed record ListActivePalaceLawDefinitionsResponse(
    IReadOnlyCollection<PalaceLawDefinitionDto> Definitions);