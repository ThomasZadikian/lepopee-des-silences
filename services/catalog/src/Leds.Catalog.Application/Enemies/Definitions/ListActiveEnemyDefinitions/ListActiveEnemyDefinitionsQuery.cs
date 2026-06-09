using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Enemies.Definitions.ListActiveEnemyDefinitions;

public sealed record ListActiveEnemyDefinitionsQuery()
    : IQuery<ListActiveEnemyDefinitionsResponse>;
