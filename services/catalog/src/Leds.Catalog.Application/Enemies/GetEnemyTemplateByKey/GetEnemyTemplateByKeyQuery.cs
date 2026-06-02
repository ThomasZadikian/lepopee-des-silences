using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Enemies.GetEnemyTemplateByKey;

public sealed record GetEnemyTemplateByKeyQuery(string Key)
    : IQuery<GetEnemyTemplateByKeyResponse>;