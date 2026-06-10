namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class MapNodeEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Lane { get; set; }
    public int RiskLevel { get; set; }
    public string RewardProfile { get; set; } = string.Empty;
    public bool IsBoss { get; set; }
    public string State { get; set; } = string.Empty;
    public string? ChosenEventOptionId { get; set; }

    public RoomEntity? Room { get; set; }
    public List<MapNodeParentNodeEntity> ParentNodeLinks { get; set; } = [];
}
