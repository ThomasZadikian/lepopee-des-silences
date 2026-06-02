namespace Leds.SharedBuildingBlocks.Time;

/// <summary>
/// Provides the current time.
/// Used to avoid direct DateTimeOffset.UtcNow calls inside services.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
} 