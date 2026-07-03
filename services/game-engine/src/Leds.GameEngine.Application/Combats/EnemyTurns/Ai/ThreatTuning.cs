namespace Leds.GameEngine.Application.Combats.EnemyTurns.Ai;

/// <summary>
/// Tuning constants for the threat/aggro system — how much a player action
/// contributes to the acting combatant's <see cref="Combatant.ThreatValue"/>,
/// and how heavily the enemy targeting scorer weighs accrued threat.
/// Centralized here so the numbers are one greppable, tunable surface instead
/// of scattered magic numbers across the resolver and the planner.
/// </summary>
internal static class ThreatTuning
{
    /// <summary>Threat per point of vitality damage dealt to an enemy.</summary>
    public const double ThreatPerVitalityDamage = 1.0;

    /// <summary>Threat per point of damage absorbed by an enemy's guard.</summary>
    public const double ThreatPerGuardAbsorbed = 0.5;

    /// <summary>Threat per point of vitality healed on an ally.</summary>
    public const double ThreatPerHealing = 0.8;

    /// <summary>Threat per point of guard granted to an ally.</summary>
    public const double ThreatPerGuardGranted = 0.5;

    /// <summary>Weight of normalized threat share in the enemy target-selection score.</summary>
    public const double TargetThreatWeight = 35.0;

    /// <summary>Bonus added when a candidate target is the actor's own last attacker.</summary>
    public const double TargetLastAttackerBonus = 20.0;

    /// <summary>Weight of the deterministic tie-break jitter (must stay well below any real scoring delta).</summary>
    public const double TargetJitterWeight = 0.001;
}
