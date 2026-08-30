namespace Leds.GameEngine.Domain.Npcs;

public readonly record struct RoomNpcId(Guid Value)
{
    public static RoomNpcId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
