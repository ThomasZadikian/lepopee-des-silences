using Leds.Catalog.Application.Enemies.Definitions.Dtos;

namespace Leds.Catalog.Application.Enemies.Definitions.ListActiveEnemyDefinitions;

public sealed record ListActiveEnemyDefinitionsResponse(
    IReadOnlyCollection<EnemyDefinitionDto> Definitions);
