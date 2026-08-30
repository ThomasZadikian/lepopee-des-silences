namespace Leds.Player.Domain.Players;

public readonly record struct PlayerCharacterId(Guid Value)
{
    public static PlayerCharacterId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
