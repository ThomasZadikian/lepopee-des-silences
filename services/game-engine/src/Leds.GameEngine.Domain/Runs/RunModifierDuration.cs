namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// Defines when a <see cref="RunModifier"/> is consumed and removed from the run.
/// </summary>
public enum RunModifierDuration
{
    /// <summary>Modifier persists for the entire run and is never automatically consumed.</summary>
    UntilRunEnds = 0,

    /// <summary>Modifier is consumed after the next combat resolves.</summary>
    NextCombatOnly = 1,

    /// <summary>Modifier is consumed at the end of the current room.</summary>
    UntilRoomEnds = 2,
}