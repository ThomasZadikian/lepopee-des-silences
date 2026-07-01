using Leds.Catalog.Application.Enemies.Loot.Dtos;
using Leds.Catalog.Domain.Rewards.Loot;

namespace Leds.Catalog.Application.Rewards.GenericLoot.Dtos;

public sealed record GenericLootPoolDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    IReadOnlyCollection<LootEntryDto> Entries)
{
    public static GenericLootPoolDto FromDomain(IGenericLootPool pool) => new(
        pool.Id.Value,
        pool.Key.Value,
        pool.Name.Value,
        pool.Description.Value,
        pool.Version.Value,
        pool.Status.ToString(),
        pool.Entries.Select(LootEntryDto.FromDomain).ToArray());
}
