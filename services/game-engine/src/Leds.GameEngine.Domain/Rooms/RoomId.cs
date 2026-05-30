namespace Leds.GameEngine.Domain.Rooms;

public readonly record struct RoomId(Guid Value)
{
    public static RoomId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}