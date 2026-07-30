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
    public int Focus { get; set; }
    public int MagicAttack { get; set; }
    public int MagicDefense { get; set; }
    public int RunItemCapacity { get; set; }
    public string? TypedDamageReductionsJson { get; set; }
    public int HitChanceBonusPercent { get; set; }
    public int DotDurationReductionPercent { get; set; }
    public int DotDamageReductionPercent { get; set; }
    public int DotDamageBonusPercent { get; set; }
    public int MagicDamageBonusPercent { get; set; }
    public int MagicDamageReductionPercent { get; set; }
    public int CriticalChanceBonusPercent { get; set; }
    public int GuardBonusPercent { get; set; }
    public bool JournalEnabled { get; set; }
    public bool LawDenialEnabled { get; set; }

    public int? LawDenialLastUsedRoomIndex { get; set; }
    public int? LastPromulgationFloorIndex { get; set; }
    public string? ForgottenSkillKey { get; set; }
    public string? SuspendedSevereLawModifierIdsJson { get; set; }
    public int ReputationGainBonusPercent { get; set; }
    public bool HimLitProtectionEnabled { get; set; }
    public int HealingBonusPercent { get; set; }
    public bool CaliceInfiniEnabled { get; set; }
    public int? CaliceInfiniLastUsedRoomIndex { get; set; }
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
    public string? SnapshotRunItemIds { get; set; }
    public string? SnapshotRunModifierIds { get; set; }
    public string? ActiveNpcKey { get; set; }
    public string? NpcRelationshipsJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<RoomEntity> Rooms { get; set; } = [];
    public List<RunMemoryFragmentEntity> MemoryFragments { get; set; } = [];
    public List<RunJournalEntryEntity> JournalEntries { get; set; } = [];
    public List<RunActivePalaceLawEntity> ActivePalaceLaws { get; set; } = [];
    public List<ActiveCurseEntity> ActiveCurses { get; set; } = [];
    public List<RunItemEntity> InventoryItems { get; set; } = [];
    public List<RunModifierEntity> RunModifiers { get; set; } = [];
    public CombatEntity? ActiveCombat { get; set; }
    public PlayerRuntimeStateEntity? PlayerState { get; set; }
    public RunPlayerSnapshotEntity? PlayerSnapshot { get; set; }
}
