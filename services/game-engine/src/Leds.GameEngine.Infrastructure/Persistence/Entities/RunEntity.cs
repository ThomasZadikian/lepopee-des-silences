namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Seed { get; set; } = string.Empty;
    public string GeneratorVersion { get; set; } = string.Empty;
    public string MarkovMatrixVersion { get; set; } = string.Empty;
    public Guid CurrentRoomId { get; set; }
    public int CurrentRoomIndex { get; set; }
    public Guid? ActiveCombatId { get; set; }
    public Guid? PendingRewardOfferId { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public DateTime? SavedAtUtc { get; set; }
    public string? PreSuspendStatus { get; set; }
    public int? SnapshotCurrentHp { get; set; }
    public int? SnapshotAttack { get; set; }
    public int? SnapshotDefense { get; set; }
    public int? SnapshotSpeed { get; set; }
    public string? SnapshotMemoryFragments { get; set; }
    public string? SnapshotActivePalaceLaws { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<RoomEntity> Rooms { get; set; } = [];
    public List<RunMemoryFragmentEntity> MemoryFragments { get; set; } = [];
    public List<RunActivePalaceLawEntity> ActivePalaceLaws { get; set; } = [];
}
