using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Rewards.Loot;

// Fallback, non-enemy-specific pool used to pad a combat's rolled loot up to the
// 3-item floor when the enemies present didn't roll enough on their own.
public sealed class GenericLootPool : CatalogContentBase, IGenericLootPool
{
    private readonly List<LootEntry> _entries;

    private GenericLootPool(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        IReadOnlyCollection<LootEntry> entries)
        : base(id, key, name, description, version, status)
    {
        _entries = entries.ToList();
    }

    public IReadOnlyCollection<LootEntry> Entries => _entries.AsReadOnly();

    public static GenericLootPool Create(
        string key,
        string name,
        string? description,
        string version,
        IReadOnlyCollection<LootEntry> entries,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        if (entries.Count == 0)
        {
            throw new DomainException("Generic loot pool must contain at least one entry.");
        }

        if (entries.Select(e => e.ItemDefinitionKey).Distinct().Count() != entries.Count)
        {
            throw new DomainException("Generic loot pool cannot contain duplicate item definition keys.");
        }

        return new GenericLootPool(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            CatalogContentDescription.From(description),
            CatalogContentVersion.From(version),
            status,
            entries.ToArray());
    }
}
