using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Worlds.ListActiveWorldDefinitions;

public sealed record ListActiveWorldDefinitionsQuery() : IQuery<ListActiveWorldDefinitionsResponse>;
