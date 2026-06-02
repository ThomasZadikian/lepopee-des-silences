using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.EventTemplates.GetEventTemplateByKey;

public sealed record GetEventTemplateByKeyQuery(string Key)
    : IQuery<GetEventTemplateByKeyResponse>;