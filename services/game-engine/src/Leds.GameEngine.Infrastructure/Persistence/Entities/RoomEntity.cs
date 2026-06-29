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

    public RunEntity? Run { get; set; }
    public List<MapNodeEntity> Nodes { get; set; } = [];
}
