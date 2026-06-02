using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Enemies.ListActiveEnemyTemplates;

public sealed record ListActiveEnemyTemplatesQuery()
    : IQuery<ListActiveEnemyTemplatesResponse>;