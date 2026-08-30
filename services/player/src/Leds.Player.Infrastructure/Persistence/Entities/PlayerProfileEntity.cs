namespace Leds.Player.Infrastructure.Persistence.Entities;

public sealed class PlayerProfileEntity
{
    public Guid Id { get; set; }
    public string? AuthSubjectId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int TotalRunsStarted { get; set; }
    public int TotalRunsCompleted { get; set; }
    public int TotalRunsFailed { get; set; }
    public int TotalRunsAbandoned { get; set; }
    public int UnspentStatPoints { get; set; }
    public int TotalStatPointsEarned { get; set; }
    public int PalaceShardCount { get; set; }
    public int HimLitShardCount { get; set; }
    public string? MainStorySequenceKey { get; set; }
    public string? MainStorySequenceVersion { get; set; }
    public string? MainStoryStepKey { get; set; }
    public string? MainStoryCheckpointKey { get; set; }
    public bool MainStoryCompleted { get; set; }
    public int HighestDifficultyLevelUnlocked { get; set; }
    public string MainStoryUnlockedRoomKeysJson { get; set; } = "[]";
    public string MainStoryVisibleRoomKeysJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<PlayerCharacterEntity> Characters { get; set; } = [];
    public List<PlayerPermanentUnlockEntity> PermanentUnlocks { get; set; } = [];
    public List<PlayerPermanentItemEntity> PermanentItems { get; set; } = [];
    public List<PlayerRunStatisticEntity> RunStatistics { get; set; } = [];
    public List<PlayerNpcReputationScoreEntity> NpcReputationScores { get; set; } = [];
}
