namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class ActiveCurseEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double DifficultyDelta { get; set; }
    public DateTime AppliedAtUtc { get; set; }
    public string? CurseDefinitionKey { get; set; }
    public int Severity { get; set; }
    public string Duration { get; set; } = "NextCombatOnly";
    public Guid? ExpiresAtRoomId { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public string? EffectSetKey { get; set; }

    public RunEntity? Run { get; set; }
}
