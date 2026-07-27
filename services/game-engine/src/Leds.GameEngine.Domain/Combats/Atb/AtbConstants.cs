namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Tunable constants for the ATB (Active Time Battle) scheduler. Integer math
/// throughout for cross-platform deterministic reproducibility (no doubles in
/// the gauge loop — see <see cref="AtbScheduler"/>).
/// </summary>
public static class AtbConstants
{
    /// <summary>Gauge value a combatant must reach to be allowed to act. Also the cap — a ready gauge does not climb further.</summary>
    public const int ReadyThreshold = 50_000;

    /// <summary>Initiative stat → starting gauge units (opening stagger).</summary>
    public const int InitiativeScale = 100;

    /// <summary>Safety bound so a degenerate state (all fills 0) cannot loop forever.</summary>
    public const long MaxTicksPerAdvance = 75000;

    /// <summary>
    /// Authoring convention only — NOT a scheduler concept. "1 tour" in skill/status
    /// design (durations, DoT/HoT tick counts) means this many ticks of the ATB clock.
    /// It does NOT mean "1 action taken": because <see cref="AtbTempoFormula"/> makes
    /// fill-per-tick vary per combatant (Speed, investment, relative tempo, momentum),
    /// a fast combatant can act many times within "N tours" while a slow one acts once
    /// or not at all. Single source of truth for this conversion on the game-engine
    /// side — mirror kept in sync in the catalog service's CatalogSeedRunner (a
    /// separate deployable that cannot reference this constant directly).
    /// </summary>
    /// <remarks>
    /// Délègue désormais à <see cref="CombatTime.TicksPerTurn"/> : l'échelle du temps de combat
    /// est commune aux deux systèmes, elle n'appartient pas à l'ATB. Conservé ici pour ne pas
    /// toucher aux points d'appel existants, et parce que c'est le nom que le code ATB emploie.
    /// </remarks>
    public const int TicksPerTurn = CombatTime.TicksPerTurn;
}