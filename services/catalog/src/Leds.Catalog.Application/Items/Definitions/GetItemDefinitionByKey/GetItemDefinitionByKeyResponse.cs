using Leds.Catalog.Application.Items.Definitions.Dtos;

namespace Leds.Catalog.Application.Items.Definitions.GetItemDefinitionByKey;

public sealed record GetItemDefinitionByKeyResponse(
    ItemDefinitionDto? Definition);
