namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class CombatantRuntimeStateEntity
{
    public Guid Id { get; set; }
    public Guid CombatantId { get; set; }
    public int CurrentVitality { get; set; }
    public int CurrentGuard { get; set; }
    public int CurrentFocus { get; set; }
    public int CurrentMana { get; set; }
    public int CurrentCharge { get; set; }
    public int? AtbGaugeValue { get; set; }
    public int? ActionRecoveryUntilTick { get; set; }
    public int? AtbFillPerTick { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public CombatantEntity? Combatant { get; set; }
}
