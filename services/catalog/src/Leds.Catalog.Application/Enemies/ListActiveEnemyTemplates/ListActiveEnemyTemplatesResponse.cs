using Leds.Catalog.Application.Enemies.Dtos;

namespace Leds.Catalog.Application.Enemies.ListActiveEnemyTemplates;

public sealed record ListActiveEnemyTemplatesResponse(
    IReadOnlyCollection<EnemyTemplateDto> Templates);