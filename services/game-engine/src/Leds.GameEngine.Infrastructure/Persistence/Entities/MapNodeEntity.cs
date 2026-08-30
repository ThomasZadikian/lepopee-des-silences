namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class MapNodeEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Lane { get; set; }
    public int RiskLevel { get; set; }
    public string? CombatRiskTier { get; set; }
    public string RewardProfile { get; set; } = string.Empty;
    public bool IsBoss { get; set; }
    public string State { get; set; } = string.Empty;
    public string? ChosenEventOptionId { get; set; }

    /// <summary>Name of the HiddenState enum value (None/Hint/Revealed).</summary>
    public string HiddenState { get; set; } = "None";

    /// <summary>Name of the DangerTell enum value (None/Tracks/Glow/Blight).</summary>
    public string DangerTell { get; set; } = "None";

    /// <summary>Name of the ContactBehavior enum value (None/TriggerOnEnter/Blocking).</summary>
    public string ContactBehavior { get; set; } = "None";

    /// <summary>Exit nodes only — the catalog room this exit leads to. Null is meaningful
    /// (legacy content with no reachability graph); see MapNode.ExitDestinationRoomKey.</summary>
    public string? ExitDestinationRoomKey { get; set; }

    /// <summary>Exit nodes only — cached display name for <see cref="ExitDestinationRoomKey"/>.</summary>
    public string? ExitDestinationDisplayName { get; set; }

    public RoomEntity? Room { get; set; }
    public List<MapNodeParentNodeEntity> ParentNodeLinks { get; set; } = [];
}