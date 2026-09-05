namespace Leds.Player.Domain.Players;

public readonly record struct OwnedItemInstanceId(Guid Value)
{
    public static OwnedItemInstanceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
