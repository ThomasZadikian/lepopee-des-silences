namespace Leds.GameEngine.Domain.PalaceLaws;

/// <summary>
/// Canonical polarity tier strings for palace laws — drives the Soupape rule
/// (<see cref="Runs.Run.ShouldForceCompliantPromulgation"/>) and the exclusion/majeure
/// checks in <see cref="Runs.Run.PromulgateLaw"/>.
/// </summary>
public static class PalaceLawPolarity
{
    public const string Clemente = "Clémente";
    public const string Severe = "Sévère";
    public const string DoubleTranchant = "DoubleTranchant";
    public const string Neutre = "Neutre";
}
