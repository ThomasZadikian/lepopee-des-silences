namespace Leds.GameEngine.Domain.Rooms;

public enum RoomState
{
    Active = 0,
    NodeSelected = 1,
    NodeResolved = 2,
    BossReached = 3,

    /// <summary>
    /// No longer produced — the boss stopped gating room progression (see
    /// Room.ResolveSelectedNodeEvent, Run.ConfirmRoomExit). Kept, not removed, because
    /// RoomState is persisted as a string (RunPersistenceMapper) and deleting the member
    /// would crash Rehydrate for any row still parked at this value.
    /// </summary>
    [Obsolete("No longer produced; kept for backward Rehydrate compatibility.")]
    Completed = 4
}