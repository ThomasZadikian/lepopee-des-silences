namespace Leds.GameEngine.Domain.Runs;

public enum RunStatus
{
    Created = 0,
    Active = 1,
    RoomResolved = 2,
    BossReached = 3,
    Completed = 4,
    Failed = 5,
    Abandoned = 6,

    /// <summary>
    /// Run is in the Interlude phase between two rooms.
    /// The boss of the current room has been defeated and its reward selected.
    /// The player is navigating the Interlude before entering the next room.
    /// </summary>
    Interlude = 7,

    /// <summary>
    /// Run has been saved and the player has returned to the menu.
    /// The run is paused at a safe point (RoomResolved or Interlude) and can be resumed.
    /// No game actions are permitted until the player resumes the run.
    /// </summary>
    Suspended = 8
}