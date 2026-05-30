namespace Leds.GameEngine.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}