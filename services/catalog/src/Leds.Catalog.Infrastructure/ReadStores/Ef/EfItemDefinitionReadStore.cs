using Leds.Catalog.Application.Items.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Items;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfItemDefinitionReadStore : IItemTemplateReadStore
{
    private readonly CatalogDbContext _context;

    public EfItemDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<IItemTemplate>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.ItemDefinitions
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IItemTemplate?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.ItemDefinitions
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    private static IItemTemplate MapToDomain(ItemDefinitionEntity entity)
    {
        return ItemTemplate.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            Enum.Parse<ItemCategory>(entity.Category),
            Enum.Parse<ItemRarity>(entity.Rarity),
            Enum.Parse<ItemDuration>(entity.Duration),
            entity.EffectValue,
            entity.Price,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
