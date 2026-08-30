namespace Leds.GameEngine.Domain.Effects;

public enum EffectDuration
{
    Immediate,
    CurrentCombat,
    NextCombatOnly,
    NextRewardOnly,
    UntilRoomEnds,
    UntilRunEnds,
    PermanentCandidate,
    UntilConsumed,
}
