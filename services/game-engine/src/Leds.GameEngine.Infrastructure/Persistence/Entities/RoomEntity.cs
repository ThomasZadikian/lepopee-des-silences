namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RoomEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public int Depth { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public string PalaceState { get; set; } = "Neutral";
    public string Theme { get; set; } = string.Empty;
    public string BossId { get; set; } = string.Empty;
    public string BossName { get; set; } = string.Empty;
    public string BossRoomType { get; set; } = string.Empty;
    public string BossDangerHint { get; set; } = string.Empty;
    public string BossEnemyTemplateKey { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int CurrentNodeDepth { get; set; }
    public int MaxNodeDepth { get; set; }
    public string? LayoutTemplateKey { get; set; }
    public string? LayoutTemplateVersion { get; set; }
    public string? CatalogRoomKey { get; set; }
    public string? CatalogDisplayName { get; set; }
    public string? CatalogNarrativeText { get; set; }
    public string? EnemyPoolKey { get; set; }
    public string? RewardPoolKey { get; set; }
    public string? LawPoolKey { get; set; }
    public string? CursePoolKey { get; set; }
    public bool CatalogIsUnique { get; set; }

    public int GridWidth { get; set; }
    public int GridHeight { get; set; }
    public int GridMovementBudget { get; set; }
    public int GridMovementBudgetRemaining { get; set; }
    public int GridStartX { get; set; }
    public int GridStartY { get; set; }
    public int GridPartyX { get; set; }
    public int GridPartyY { get; set; }
    /// <summary>Semicolon-separated node GUIDs, e.g. "guid1;guid2".</summary>
    public string GridRevealedNodeIdsCsv { get; set; } = string.Empty;
    /// <summary>Semicolon-separated "x,y" pairs, e.g. "0,0;1,0;2,0".</summary>
    public string GridRevealedCellsCsv { get; set; } = string.Empty;
    /// <summary>Comma-separated ints, row-major (index = y*GridWidth+x), one per cell.</summary>
    public string GridElevationCsv { get; set; } = string.Empty;
    /// <summary>Semicolon-separated "x,y" pairs, same format as GridRevealedCellsCsv.</summary>
    public string GridObstacleCellsCsv { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated 0/1, row-major (index = y*GridWidth+x), one per cell: which cells are
    /// part of the room. Empty means "every cell is floor" — the shape rooms had before rooms
    /// had a shape, which is exactly how rows persisted before this column should be read.
    /// </summary>
    public string GridFloorCellsCsv { get; set; } = string.Empty;
    /// <summary>Semicolon-separated "x,y" pairs, same format as GridRevealedCellsCsv. Empty for
    /// most rooms — only a sub-room's threshold is a door.</summary>
    public string GridDoorCellsCsv { get; set; } = string.Empty;
    /// <summary>Semicolon-separated "x,y,key" triples, e.g. "10,4,carpet;11,4,carpet". Empty for
    /// every procedurally-generated room — only an authored room (the Hall) has one today.</summary>
    public string GridSurfaceOverridesCsv { get; set; } = string.Empty;
    /// <summary>Same "x,y,key" format as GridSurfaceOverridesCsv, for authored decor placement
    /// (e.g. the Hall's four marble pillars).</summary>
    public string GridDecorPlacementsCsv { get; set; } = string.Empty;
    /// <summary>The node currently in the Select/Resolve interaction slot — see Room._currentGridNodeId.</summary>
    public Guid? CurrentGridNodeId { get; set; }

    public RunEntity? Run { get; set; }
    public List<MapNodeEntity> Nodes { get; set; } = [];
    public List<RoomNpcEntity> RoomNpcs { get; set; } = [];
    public List<LocalRuleStateEntity> LocalRuleStates { get; set; } = [];
}
