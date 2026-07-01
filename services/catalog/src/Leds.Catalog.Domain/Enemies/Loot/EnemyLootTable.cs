using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Rewards.Loot;

namespace Leds.Catalog.Domain.Enemies.Loot;

// One table per enemy definition (1:1, enforced by a unique index on EnemyDefinitionKey
// at the persistence level) — matches "each enemy has its own personal loot table."
public sealed class EnemyLootTable : CatalogContentBase, IEnemyLootTable
{
    private readonly List<LootEntry> _entries;

    private EnemyLootTable(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        string enemyDefinitionKey,
        IReadOnlyCollection<LootEntry> entries)
        : base(id, key, name, description, version, status)
    {
        EnemyDefinitionKey = enemyDefinitionKey;
        _entries = entries.ToList();
    }

    public string EnemyDefinitionKey { get; }

    public IReadOnlyCollection<LootEntry> Entries => _entries.AsReadOnly();

    public static EnemyLootTable Create(
        string key,
        string name,
        string? description,
        string version,
        string enemyDefinitionKey,
        IReadOnlyCollection<LootEntry> entries,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        if (string.IsNullOrWhiteSpace(enemyDefinitionKey))
        {
            throw new DomainException("Enemy loot table must reference an enemy definition key.");
        }

        if (entries.Count == 0)
        {
            throw new DomainException("Enemy loot table must contain at least one entry.");
        }

        if (entries.Select(e => e.ItemDefinitionKey).Distinct().Count() != entries.Count)
        {
            throw new DomainException("Enemy loot table cannot contain duplicate item definition keys.");
        }

        return new EnemyLootTable(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            CatalogContentDescription.From(description),
            CatalogContentVersion.From(version),
            status,
            enemyDefinitionKey.Trim(),
            entries.ToArray());
    }
}
