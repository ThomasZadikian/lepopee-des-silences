using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.RoomBosses;

public sealed class RoomBossDefinition : CatalogContentBase, IRoomBossDefinition
{
    private readonly List<string> _tags;

    private RoomBossDefinition(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        string roomType,
        int baseDifficulty,
        IReadOnlyCollection<string> tags)
        : base(id, key, name, description, version, status)
    {
        RoomType = roomType;
        BaseDifficulty = baseDifficulty;
        _tags = tags.ToList();
    }

    public string RoomType { get; }

    public int BaseDifficulty { get; }

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public static RoomBossDefinition Create(
        string key,
        string name,
        string? description,
        string version,
        string roomType,
        int baseDifficulty,
        IReadOnlyCollection<string>? tags,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        var desc = CatalogContentDescription.From(description);

        if (desc.IsEmpty)
        {
            throw new DomainException("Room boss definition description is required.");
        }

        if (string.IsNullOrWhiteSpace(roomType))
        {
            throw new DomainException("Room boss definition room type is required.");
        }

        if (baseDifficulty <= 0)
        {
            throw new DomainException("Room boss definition base difficulty must be greater than 0.");
        }

        var distinctTags = tags is null || tags.Count == 0
            ? Array.Empty<string>()
            : tags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return new RoomBossDefinition(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            desc,
            CatalogContentVersion.From(version),
            status,
            roomType.Trim(),
            baseDifficulty,
            distinctTags);
    }
}
