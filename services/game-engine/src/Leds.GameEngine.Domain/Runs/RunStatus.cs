namespace Leds.GameEngine.Domain.Runs;

public enum RunStatus
{
    Created = 0,
    Active = 1,
    RoomResolved = 2,
    BossReached = 3,
    Completed = 4,
    Failed = 5,
    Abandoned = 6
}