using Leds.Catalog.Application.Items.Dtos;

namespace Leds.Catalog.Application.Items.ListActiveItemTemplates;

public sealed record ListActiveItemTemplatesResponse(
    IReadOnlyCollection<ItemTemplateDto> Templates);