namespace Leds.SharedBuildingBlocks.Time;

/// <summary>
/// Default system clock implementation.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}