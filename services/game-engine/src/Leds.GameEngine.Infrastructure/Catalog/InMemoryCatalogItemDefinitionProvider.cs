using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;

namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class InMemoryCatalogItemDefinitionProvider : ICatalogItemDefinitionProvider
{
    private readonly Dictionary<string, CatalogItemDefinitionSnapshot> _items = new()
    {
        ["item.consumable.minor-heal"] = new CatalogItemDefinitionSnapshot(
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
            null),
        ["item.consumable.guard-shard"] = new CatalogItemDefinitionSnapshot(
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
            null),
    };

    public void Register(CatalogItemDefinitionSnapshot item)
    {
        _items[item.Key] = item;
    }

    public Task<CatalogItemDefinitionSnapshot?> GetItemDefinitionAsync(
        string itemDefinitionKey,
        CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(itemDefinitionKey, out var result);
        return Task.FromResult(result);
    }
}
