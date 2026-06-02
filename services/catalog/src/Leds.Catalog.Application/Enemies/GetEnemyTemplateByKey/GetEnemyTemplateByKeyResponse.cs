using Leds.Catalog.Application.Enemies.Dtos;

namespace Leds.Catalog.Application.Enemies.GetEnemyTemplateByKey;

public sealed record GetEnemyTemplateByKeyResponse(
    EnemyTemplateDto? Template);