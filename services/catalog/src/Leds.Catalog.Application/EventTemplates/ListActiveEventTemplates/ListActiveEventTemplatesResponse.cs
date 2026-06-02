using Leds.Catalog.Application.EventTemplates.Dtos;

namespace Leds.Catalog.Application.EventTemplates.ListActiveEventTemplates;

public sealed record ListActiveEventTemplatesResponse(
    IReadOnlyCollection<EventTemplateDto> Templates);