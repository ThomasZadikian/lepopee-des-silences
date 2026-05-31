namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public sealed class RoomEventGenerationStateFactory : IRoomEventGenerationStateFactory
{
    public IRoomEventGenerationState Create()
    {
        return new RoomEventGenerationState();
    }
}