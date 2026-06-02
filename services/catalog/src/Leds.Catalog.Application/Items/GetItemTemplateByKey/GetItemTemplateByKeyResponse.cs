using Leds.Catalog.Application.Items.Dtos;

namespace Leds.Catalog.Application.Items.GetItemTemplateByKey;

public sealed record GetItemTemplateByKeyResponse(
    ItemTemplateDto? Template);