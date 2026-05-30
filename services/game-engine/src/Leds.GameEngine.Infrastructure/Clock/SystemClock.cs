using Leds.GameEngine.Application.Abstractions;

namespace Leds.GameEngine.Infrastructure.Clock;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}