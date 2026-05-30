namespace Leds.GameEngine.Domain.Runs;

public readonly record struct RunId(Guid Value)
{
    public static RunId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
