namespace Leds.Player.Domain.Players;

public sealed class PlayerProgression
{
    private PlayerProgression(
        int totalRunsStarted,
        int totalRunsCompleted,
        int totalRunsFailed)
    {
        TotalRunsStarted = totalRunsStarted;
        TotalRunsCompleted = totalRunsCompleted;
        TotalRunsFailed = totalRunsFailed;
    }

    public int TotalRunsStarted { get; private set; }
    public int TotalRunsCompleted { get; private set; }
    public int TotalRunsFailed { get; private set; }

    public static PlayerProgression CreateDefault()
    {
        return new PlayerProgression(0, 0, 0);
    }

    public void IncrementRunsStarted() => TotalRunsStarted++;
    public void IncrementRunsCompleted() => TotalRunsCompleted++;
    public void IncrementRunsFailed() => TotalRunsFailed++;

    /// <summary>
    /// Rehydrates player progression from a trusted persistence snapshot.
    /// This method must not be used to create new progression.
    /// </summary>
    public static PlayerProgression Rehydrate(
        int totalRunsStarted,
        int totalRunsCompleted,
        int totalRunsFailed)
    {
        return new PlayerProgression(totalRunsStarted, totalRunsCompleted, totalRunsFailed);
    }
}
