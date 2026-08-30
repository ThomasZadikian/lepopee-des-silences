using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Npcs.Definitions.ListActiveNpcDefinitions;

public sealed record ListActiveNpcDefinitionsQuery()
    : IQuery<ListActiveNpcDefinitionsResponse>;
