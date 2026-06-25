namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Tunable constants for the ATB (Active Time Battle) scheduler. Integer math
/// throughout for cross-platform deterministic reproducibility (no doubles in
/// the gauge loop — see <see cref="AtbScheduler"/>).
/// </summary>
public static class AtbConstants
{
    /// <summary>Gauge value a combatant must reach to be allowed to act.</summary>
    public const int ReadyThreshold = 50_000;

    /// <summary>
    /// How far past <see cref="ReadyThreshold"/> a held gauge may bank (charge
    /// mechanic). A gauge is capped at ReadyThreshold + MaxChargeOverflow.
    /// </summary>
    public const int MaxChargeOverflow = 10_000;

    /// <summary>Initiative stat → starting gauge units (opening stagger).</summary>
    public const int InitiativeScale = 100;

    /// <summary>Safety bound so a degenerate state (all fills 0) cannot loop forever.</summary>
    public const long MaxTicksPerAdvance = 1_000_000;
}