using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Enemies.Definitions.ListCompatibleEnemyDefinitions;

public sealed record ListCompatibleEnemyDefinitionsQuery(
    string RoomType,
    int RiskLevel)
    : IQuery<ListCompatibleEnemyDefinitionsResponse>;
