using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Items.Definitions.ListActiveItemDefinitions;

public sealed record ListActiveItemDefinitionsQuery()
    : IQuery<ListActiveItemDefinitionsResponse>;
