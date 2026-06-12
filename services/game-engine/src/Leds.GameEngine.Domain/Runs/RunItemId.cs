namespace Leds.GameEngine.Domain.Runs;

public readonly record struct RunItemId(Guid Value)
{
    public static RunItemId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
