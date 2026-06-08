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
    Interlude = 7
}