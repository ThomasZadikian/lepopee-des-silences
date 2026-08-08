namespace Leds.GameEngine.Domain.Runs;

public enum RunStatus
{
    Created = 0,
    Active = 1,

    /// <summary>
    /// No longer produced — the room boss stopped gating progression, so Run never leaves
    /// Active on node resolution anymore (see Run.ResolveCurrentEvent, Run.ConfirmRoomExit).
    /// Kept, not removed, because RunStatus is persisted as a string (RunPersistenceMapper)
    /// and deleting the member would crash Rehydrate for any row still parked at this value.
    /// </summary>
    [Obsolete("No longer produced; kept for backward Rehydrate compatibility.")]
    RoomResolved = 2,

    BossReached = 3,
    Completed = 4,
    Failed = 5,
    Abandoned = 6,

    /// <summary>
    /// No longer produced — the hub between two rooms is gone; the player now confirms a
    /// room exit directly (see Run.ConfirmRoomExit). Kept, not removed, for the same
    /// string-persistence Rehydrate reason as <see cref="RoomResolved"/>.
    /// </summary>
    [Obsolete("No longer produced; kept for backward Rehydrate compatibility.")]
    Interlude = 7,

    /// <summary>
    /// Run has been saved and the player has returned to the menu.
    /// The run is paused at a safe point (nothing pending — no active combat, no reward
    /// offer, no node mid-resolution) and can be resumed. No game actions are permitted
    /// until the player resumes the run.
    /// </summary>
    Suspended = 8
}