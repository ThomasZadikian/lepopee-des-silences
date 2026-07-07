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
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<PlayerCharacterEntity> Characters { get; set; } = [];
    public List<PlayerPermanentUnlockEntity> PermanentUnlocks { get; set; } = [];
    public List<PlayerPermanentItemEntity> PermanentItems { get; set; } = [];
    public List<PlayerRunStatisticEntity> RunStatistics { get; set; } = [];
}
