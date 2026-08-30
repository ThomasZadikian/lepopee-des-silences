namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// Business outcome of a resolved run. Lifecycle is represented independently by
/// <see cref="RunStatus"/>.
/// </summary>
public enum RunOutcome
{
    Success = 1,
    Defeat = 2,
    Abandon = 3
}
