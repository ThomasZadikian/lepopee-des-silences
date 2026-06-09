using Leds.Catalog.Application.Enemies.Definitions.Dtos;

namespace Leds.Catalog.Application.Enemies.Definitions.GetEnemyDefinitionByKey;

public sealed record GetEnemyDefinitionByKeyResponse(
    EnemyDefinitionDto? Definition);
