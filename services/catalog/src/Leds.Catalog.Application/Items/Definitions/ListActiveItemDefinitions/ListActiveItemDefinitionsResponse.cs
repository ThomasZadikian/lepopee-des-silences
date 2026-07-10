using Leds.Catalog.Application.Items.Definitions.Dtos;

namespace Leds.Catalog.Application.Items.Definitions.ListActiveItemDefinitions;

public sealed record ListActiveItemDefinitionsResponse(
    IReadOnlyCollection<ItemDefinitionDto> Definitions);
