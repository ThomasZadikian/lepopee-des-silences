using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;

namespace Leds.Catalog.Domain.Npcs;

public sealed class NpcDefinition : CatalogContentBase, INpcDefinition
{
    private readonly List<string> _tags;
    private readonly List<string> _compatibleRoomTypes;
    private readonly List<string> _compatiblePalaceRoomStates;
    private readonly List<string> _compatibleRoomClimates;

    private NpcDefinition(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<string> compatibleRoomTypes,
        IReadOnlyCollection<string> compatiblePalaceRoomStates,
        IReadOnlyCollection<string> compatibleRoomClimates,
        int? minDepth,
        int? maxDepth)
        : base(id, key, name, description, version, status)
    {
        _tags = tags.ToList();
        _compatibleRoomTypes = compatibleRoomTypes.ToList();
        _compatiblePalaceRoomStates = compatiblePalaceRoomStates.ToList();
        _compatibleRoomClimates = compatibleRoomClimates.ToList();
        MinDepth = minDepth;
        MaxDepth = maxDepth;
    }

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public IReadOnlyCollection<string> CompatibleRoomTypes => _compatibleRoomTypes.AsReadOnly();

    public IReadOnlyCollection<string> CompatiblePalaceRoomStates => _compatiblePalaceRoomStates.AsReadOnly();

    public IReadOnlyCollection<string> CompatibleRoomClimates => _compatibleRoomClimates.AsReadOnly();

    public int? MinDepth { get; }

    public int? MaxDepth { get; }

    public static NpcDefinition Create(
        string key,
        string name,
        string? description,
        string version,
        IReadOnlyCollection<string>? tags,
        IReadOnlyCollection<string>? compatibleRoomTypes,
        IReadOnlyCollection<string>? compatiblePalaceRoomStates,
        IReadOnlyCollection<string>? compatibleRoomClimates,
        int? minDepth = null,
        int? maxDepth = null,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        var desc = CatalogContentDescription.From(description);

        return new NpcDefinition(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            desc,
            CatalogContentVersion.From(version),
            status,
            tags?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            compatibleRoomTypes?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            compatiblePalaceRoomStates?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            compatibleRoomClimates?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            minDepth,
            maxDepth);
    }
}
