using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.EventTemplates.ListActiveEventTemplates;

public sealed record ListActiveEventTemplatesQuery()
    : IQuery<ListActiveEventTemplatesResponse>;