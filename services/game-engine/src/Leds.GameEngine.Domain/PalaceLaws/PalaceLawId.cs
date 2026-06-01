namespace Leds.GameEngine.Domain.PalaceLaws;

public readonly record struct PalaceLawId(Guid Value)
{
    public static PalaceLawId New()
    {
        return new PalaceLawId(Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}