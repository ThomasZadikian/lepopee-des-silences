namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class CombatActionRecordEntity
{
    public Guid Id { get; set; }
    public Guid CombatId { get; set; }
    public int TurnNumber { get; set; }
    public Guid ActorId { get; set; }
    public string ActorSide { get; set; } = string.Empty;
    public string SkillKey { get; set; } = string.Empty;
    public string? SkillDisplayName { get; set; }
    public string TargetIds { get; set; } = "[]";
    public string? SourceType { get; set; }
    public string? SourceKey { get; set; }

    public int RawDamage { get; set; }
    public int MitigatedDamage { get; set; }
    public int VitalityDamage { get; set; }
    public int GuardDamage { get; set; }
    public int GuardAbsorbed { get; set; }
    public int GuardGained { get; set; }

    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int HealingDone { get; set; }
    public int HealingReceived { get; set; }

    public string EffectsApplied { get; set; } = "[]";
    public DateTime OccurredAtUtc { get; set; }

    public CombatEntity? Combat { get; set; }
}
