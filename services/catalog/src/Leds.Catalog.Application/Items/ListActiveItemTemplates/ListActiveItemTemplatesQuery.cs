using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Items.ListActiveItemTemplates;

public sealed record ListActiveItemTemplatesQuery()
    : IQuery<ListActiveItemTemplatesResponse>;