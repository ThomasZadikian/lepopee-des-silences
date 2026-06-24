namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// The outcome of advancing the ATB clock to the next ready combatant.
/// </summary>
public sealed record AtbAdvanceResult(
    Guid? NextActorId,
    long ElapsedTicks,
    long CurrentTick,
    IReadOnlyList<AtbParticipant> Participants)
{
    public bool Stalled => NextActorId is null;
}