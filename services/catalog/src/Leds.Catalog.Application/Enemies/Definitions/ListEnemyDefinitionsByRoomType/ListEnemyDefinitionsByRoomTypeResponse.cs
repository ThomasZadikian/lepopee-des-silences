using Leds.Catalog.Application.Enemies.Definitions.Dtos;

namespace Leds.Catalog.Application.Enemies.Definitions.ListEnemyDefinitionsByRoomType;

public sealed record ListEnemyDefinitionsByRoomTypeResponse(
    IReadOnlyCollection<EnemyDefinitionDto> Definitions);
