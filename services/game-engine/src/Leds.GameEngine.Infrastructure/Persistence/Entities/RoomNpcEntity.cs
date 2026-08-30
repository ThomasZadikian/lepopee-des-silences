namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RoomNpcEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string CatalogNpcKey { get; set; } = string.Empty;
    public int OriginX { get; set; }
    public int OriginY { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Behavior { get; set; } = string.Empty;
    public string Awareness { get; set; } = string.Empty;
    public int AwarenessRadius { get; set; }
    /// <summary>Semicolon-separated "x,y" pairs, same format as RoomEntity.GridDoorCellsCsv.
    /// Empty for every non-Patrol NPC.</summary>
    public string WaypointsCsv { get; set; } = string.Empty;
    public int WaypointIndex { get; set; }
    public int StepCount { get; set; }

    public RoomEntity? Room { get; set; }
}
