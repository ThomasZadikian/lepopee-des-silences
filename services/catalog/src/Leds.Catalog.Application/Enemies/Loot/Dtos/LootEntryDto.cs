using Leds.Catalog.Domain.Rewards.Loot;

namespace Leds.Catalog.Application.Enemies.Loot.Dtos;

public sealed record LootEntryDto(string ItemDefinitionKey, int DropPercent)
{
    public static LootEntryDto FromDomain(LootEntry entry) =>
        new(entry.ItemDefinitionKey, entry.DropPercent);
}
