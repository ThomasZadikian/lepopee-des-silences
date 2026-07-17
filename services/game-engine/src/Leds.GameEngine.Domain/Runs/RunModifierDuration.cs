namespace Leds.GameEngine.Domain.Runs;

public enum RunModifierDuration
{
    UntilRunEnds = 0,
    NextCombatOnly = 1,
    UntilRoomEnds = 2,
    NextRewardOnly = 3,
    Immediate = 4,
    PermanentCandidate = 5,

    /// <summary>
    /// Active until the run's floor changes (see <see cref="Run.FloorIndex"/>) — a coarser
    /// granularity than <see cref="UntilRoomEnds"/>, used by the "étage" duration in the
    /// Compendium des Lois du Palais.
    /// </summary>
    UntilFloorEnds = 6,
}
