using Leds.Catalog.Application.Items.Definitions.Dtos;
using Leds.Catalog.Application.Items.Definitions.Ports;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryItemDefinitionReadStore : IItemDefinitionReadStore
{
    private readonly IReadOnlyDictionary<string, ItemDefinitionDto> _definitions =
        new Dictionary<string, ItemDefinitionDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["item.consumable.minor-heal"] = new ItemDefinitionDto(
                Guid.NewGuid(),
                "item.consumable.minor-heal",
                "1.0",
                "Baume de mémoire",
                "Restaure une partie de la vitalité.",
                null,
                "Consumable",
                "Heal",
                "Common",
                "UseInCombat",
                "RuntimeRunOnly",
                "Additive",
                99,
                true,
                true,
                null,
                "Active"),
            ["item.consumable.guard-shard"] = new ItemDefinitionDto(
                Guid.NewGuid(),
                "item.consumable.guard-shard",
                "1.0",
                "Éclat de garde",
                "Offre une protection permanente pendant la run.",
                null,
                "Consumable",
                "Guard",
                "Uncommon",
                "UseInCombat",
                "RuntimeRunOnly",
                "Additive",
                99,
                true,
                false,
                null,
                "Active"),
            ["item.consumable.eclat-de-garde"] = new ItemDefinitionDto(
                Guid.NewGuid(),
                "item.consumable.eclat-de-garde",
                "alpha-0.8.1",
                "Eclat de garde",
                "Renforce la garde initiale du prochain combat.",
                null,
                "Consumable",
                "Guard",
                "Common",
                "UseOnNode",
                "RuntimeRunOnly",
                "Additive",
                3,
                false,
                true,
                "effect.item.eclat-de-garde",
                "Active")
        };

    public Task<ItemDefinitionDto?> GetDtoByKeyAsync(string key, CancellationToken cancellationToken)
    {
        _definitions.TryGetValue(key, out var definition);
        return Task.FromResult(definition);
    }
}
