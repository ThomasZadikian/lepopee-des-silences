using Leds.Catalog.Application.Enemies.Definitions.Dtos;

namespace Leds.Catalog.Application.Enemies.Definitions.ListCompatibleEnemyDefinitions;

public sealed record ListCompatibleEnemyDefinitionsResponse(
    IReadOnlyCollection<EnemyDefinitionDto> Definitions);
