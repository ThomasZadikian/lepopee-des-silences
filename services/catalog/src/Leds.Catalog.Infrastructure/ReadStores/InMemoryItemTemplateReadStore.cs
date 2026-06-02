using Leds.Catalog.Application.Items.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryItemTemplateReadStore : IItemTemplateReadStore
{
    private readonly IReadOnlyCollection<IItemTemplate> _templates;

    public InMemoryItemTemplateReadStore()
    {
        _templates =
        [
            ItemTemplate.Create(
                "item-memory-potion",
                "Potion de Mémoire",
                "Restaure une ressource mentale pendant la run.",
                "catalog-0.1.0",
                ItemCategory.Consumable,
                ItemRarity.Common,
                ItemDuration.RunOnly,
                effectValue: 25,
                price: 10,
                status: CatalogContentStatus.Active),

            ItemTemplate.Create(
                "item-silent-relic",
                "Relique du Silence",
                "Une trace permanente du Palais.",
                "catalog-0.1.0",
                ItemCategory.Relic,
                ItemRarity.Rare,
                ItemDuration.Permanent,
                effectValue: 1,
                price: 0,
                status: CatalogContentStatus.Active)
        ];
    }

    public Task<IReadOnlyCollection<IItemTemplate>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var templates = _templates
            .Where(template => template.IsActive)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IItemTemplate>>(templates);
    }

    public Task<IItemTemplate?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var template = _templates.SingleOrDefault(template =>
            string.Equals(
                template.Key.Value,
                key,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(template);
    }
}