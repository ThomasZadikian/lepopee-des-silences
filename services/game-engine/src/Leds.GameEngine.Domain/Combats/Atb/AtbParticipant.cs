namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Immutable snapshot of one combatant's ATB state fed to the scheduler.
/// The fill rate already bakes in speed × Markov tempo (room × per-combatant),
/// computed one layer up so the scheduler stays pure integer math.
/// </summary>
public sealed record AtbParticipant(
    Guid CombatantId,
    int Gauge,
    int FillPerTick,
    long RecoveryUntilTick,
    int Initiative,
    bool IsActive)
{
    public bool IsReady => Gauge >= AtbConstants.ReadyThreshold;
}