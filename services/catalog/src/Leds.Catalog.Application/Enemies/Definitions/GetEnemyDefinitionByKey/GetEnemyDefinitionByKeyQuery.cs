using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Enemies.Definitions.GetEnemyDefinitionByKey;

public sealed record GetEnemyDefinitionByKeyQuery(string Key)
    : IQuery<GetEnemyDefinitionByKeyResponse>;
