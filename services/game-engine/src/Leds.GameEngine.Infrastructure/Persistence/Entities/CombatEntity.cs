namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class CombatEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid RoomId { get; set; }
    public Guid NodeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public int CurrentTick { get; set; }
    public int HitCounter { get; set; }
    public bool HitCounterDoubleDamageEnabled { get; set; }
    public bool FirstHitCriticalEnabled { get; set; }
    public bool HasFirstHitLanded { get; set; }
    public bool LowHpDamageAmplificationEnabled { get; set; }
    public int DotDurationExtensionTicks { get; set; }
    public bool DuelDamageAsymmetryEnabled { get; set; }
    public int DotMagnitudeBonus { get; set; }
    public bool HealingBlocked { get; set; }
    public bool FalaiseWindEnabled { get; set; }
    public bool PostDeathBasicAttackOnlyEnabled { get; set; }
    public bool NextActionRestrictedToBasicAttack { get; set; }
    public bool TapisPropreEnabled { get; set; }
    public bool ThirdCupHealCorruptionEnabled { get; set; }
    public bool PresentationsEnabled { get; set; }
    public bool MiroirEnabled { get; set; }
    public bool HasMirrorTriggered { get; set; }
    public string? ForgottenSkillKey { get; set; }
    public Guid? ActiveCombatantId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public RunEntity? Run { get; set; }
    public List<CombatantEntity> Combatants { get; set; } = [];
}