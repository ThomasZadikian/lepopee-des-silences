namespace Leds.GameEngine.Domain.Protocol;

public readonly record struct LocalRuleId(Guid Value)
{
    public static LocalRuleId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
