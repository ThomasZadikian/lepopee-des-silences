using Leds.Catalog.Application.EventTemplates.Dtos;

namespace Leds.Catalog.Application.EventTemplates.GetEventTemplateByKey;

public sealed record GetEventTemplateByKeyResponse(
    EventTemplateDto? Template);