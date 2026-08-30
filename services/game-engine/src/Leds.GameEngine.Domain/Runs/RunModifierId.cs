namespace Leds.GameEngine.Domain.Runs;

public readonly record struct RunModifierId(Guid Value)
{
    public static RunModifierId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}