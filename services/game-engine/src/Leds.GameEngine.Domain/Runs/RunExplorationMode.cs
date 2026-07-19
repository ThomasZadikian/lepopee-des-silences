namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// The exploration/navigation system used for every room of a run, chosen once at
/// <see cref="Run.StartNew"/> and fixed for the run's entire duration. Combat itself
/// (ATB) is unaffected by this choice — only how the player navigates a room's nodes.
/// </summary>
public enum RunExplorationMode
{
    /// <summary>The existing row-by-row node graph (branching path, siblings lock on choice).</summary>
    Classic = 0,

    /// <summary>Free movement on a bounded grid with a movement budget and fog of war.</summary>
    Tactical = 1
}
