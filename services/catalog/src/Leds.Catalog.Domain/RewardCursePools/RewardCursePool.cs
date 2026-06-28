using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;

namespace Leds.Catalog.Domain.RewardCursePools;

public sealed class RewardCursePool : CatalogContentBase, IRewardCursePool
{
    private readonly List<RewardCurseEntry> _entries;

    private RewardCursePool(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        IReadOnlyCollection<RewardCurseEntry> entries)
        : base(id, key, name, description, version, status)
    {
        _entries = entries.ToList();
    }

    public IReadOnlyCollection<RewardCurseEntry> Entries => _entries.AsReadOnly();

    public static RewardCursePool Create(
        string key,
        string name,
        string? description,
        string version,
        IReadOnlyCollection<RewardCurseEntry>? entries = null,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        return new RewardCursePool(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            CatalogContentDescription.From(description),
            CatalogContentVersion.From(version),
            status,
            entries?.ToArray() ?? []);
    }
}