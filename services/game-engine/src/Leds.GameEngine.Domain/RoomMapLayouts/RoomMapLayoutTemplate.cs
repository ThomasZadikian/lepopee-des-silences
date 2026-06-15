using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Domain.RoomMapLayouts;

public sealed record RoomMapLayoutTemplate
{
    public RoomMapLayoutTemplate(
        string key,
        string version,
        RoomType roomType,
        IReadOnlyList<int> rowNodeCounts)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("RoomMapLayoutTemplate key is required.");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new DomainException("RoomMapLayoutTemplate version is required.");
        }

        ArgumentNullException.ThrowIfNull(rowNodeCounts);

        if (rowNodeCounts.Count < 2)
        {
            throw new DomainException("RoomMapLayoutTemplate must have at least 2 rows.");
        }

        if (rowNodeCounts.Any(count => count < 1))
        {
            throw new DomainException("Each row must have at least 1 MapNode.");
        }

        if (rowNodeCounts.Last() != 1)
        {
            throw new DomainException("The final row must contain exactly 1 MapNode (the boss).");
        }

        Key = key.Trim();
        Version = version.Trim();
        RoomType = roomType;
        RowNodeCounts = rowNodeCounts.ToArray();
        TotalNodeCount = rowNodeCounts.Sum();
        InitialRowIndex = 0;
        BossRowIndex = rowNodeCounts.Count - 1;
    }

    public string Key { get; }

    public string Version { get; }

    public RoomType RoomType { get; }

    public IReadOnlyList<int> RowNodeCounts { get; }

    public int TotalNodeCount { get; }

    public int InitialRowIndex { get; }

    public int BossRowIndex { get; }
}