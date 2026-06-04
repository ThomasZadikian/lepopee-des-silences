namespace Leds.GameEngine.Domain.Combats;

public readonly record struct CombatId(Guid Value)
{
    public static CombatId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}