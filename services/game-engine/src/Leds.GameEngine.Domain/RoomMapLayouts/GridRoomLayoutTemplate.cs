using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Domain.RoomMapLayouts;

/// <summary>
/// Tactical-mode counterpart of <see cref="RoomMapLayoutTemplate"/> — describes a bounded
/// free-movement grid instead of a row/lane DAG shape.
/// </summary>
public sealed record GridRoomLayoutTemplate
{
    public GridRoomLayoutTemplate(
        string key,
        string version,
        RoomType roomType,
        int width,
        int height,
        int movementBudget,
        int minNodeCount,
        int maxNodeCount,
        int startX,
        int startY)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("GridRoomLayoutTemplate key is required.");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new DomainException("GridRoomLayoutTemplate version is required.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new DomainException("GridRoomLayoutTemplate width and height must be greater than 0.");
        }

        if (movementBudget < 0)
        {
            throw new DomainException("GridRoomLayoutTemplate movement budget must be greater than or equal to 0.");
        }

        if (minNodeCount < 1)
        {
            throw new DomainException("GridRoomLayoutTemplate must allow at least 1 content node.");
        }

        if (maxNodeCount < minNodeCount)
        {
            throw new DomainException("GridRoomLayoutTemplate max node count must be greater than or equal to the minimum.");
        }

        if (maxNodeCount > width * height)
        {
            throw new DomainException("GridRoomLayoutTemplate max node count cannot exceed the grid's cell count.");
        }

        if (startX < 0 || startX >= width || startY < 0 || startY >= height)
        {
            throw new DomainException("GridRoomLayoutTemplate start position must be within the grid bounds.");
        }

        Key = key.Trim();
        Version = version.Trim();
        RoomType = roomType;
        Width = width;
        Height = height;
        MovementBudget = movementBudget;
        MinNodeCount = minNodeCount;
        MaxNodeCount = maxNodeCount;
        StartX = startX;
        StartY = startY;
    }

    public string Key { get; }

    public string Version { get; }

    public RoomType RoomType { get; }

    public int Width { get; }

    public int Height { get; }

    public int MovementBudget { get; }

    /// <summary>Includes an optional authored boss node when present.</summary>
    public int MinNodeCount { get; }

    /// <summary>Includes an optional authored boss node when present.</summary>
    public int MaxNodeCount { get; }

    public int StartX { get; }

    public int StartY { get; }
}
