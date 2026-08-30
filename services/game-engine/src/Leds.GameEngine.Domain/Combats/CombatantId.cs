namespace Leds.GameEngine.Domain.Combats;

public readonly record struct CombatantId(Guid Value)
{
    public static CombatantId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}