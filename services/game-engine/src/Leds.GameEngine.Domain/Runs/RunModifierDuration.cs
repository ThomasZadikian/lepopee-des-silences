namespace Leds.GameEngine.Domain.Runs;

public enum RunModifierDuration
{
    UntilRunEnds = 0,
    NextCombatOnly = 1,
    UntilRoomEnds = 2,
    NextRewardOnly = 3,
    Immediate = 4,
    PermanentCandidate = 5,
}
