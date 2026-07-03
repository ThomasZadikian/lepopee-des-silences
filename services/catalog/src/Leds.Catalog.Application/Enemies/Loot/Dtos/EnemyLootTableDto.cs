using Leds.Catalog.Domain.Enemies.Loot;

namespace Leds.Catalog.Application.Enemies.Loot.Dtos;

public sealed record EnemyLootTableDto(
    Guid Id,
    string Key,
    string EnemyDefinitionKey,
    string Name,
    string Description,
    string Version,
    string Status,
    IReadOnlyCollection<LootEntryDto> Entries)
{
    public static EnemyLootTableDto FromDomain(IEnemyLootTable table) => new(
        table.Id.Value,
        table.Key.Value,
        table.EnemyDefinitionKey,
        table.Name.Value,
        table.Description.Value,
        table.Version.Value,
        table.Status.ToString(),
        table.Entries.Select(LootEntryDto.FromDomain).ToArray());
}
