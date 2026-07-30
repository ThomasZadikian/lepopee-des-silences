namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// Le système de combat stocké avec une run.
/// </summary>
/// <remarks>
/// <para>
/// Le mode tactique est le seul mode proposé aux joueurs. La valeur ATB reste temporairement
/// lisible pour permettre le traitement explicite des sauvegardes historiques pendant la
/// transition.
/// </para>
/// <para>
/// Une run déjà engagée ne change jamais de moteur de combat en cours de partie.
/// </para>
/// </remarks>
public enum RunCombatMode
{
    /// <summary>
    /// Ancien moteur à barre de temps, conservé uniquement pour identifier les sauvegardes legacy.
    /// </summary>
    Atb = 0,

    /// <summary>
    /// Tactique tour par tour sur la grille d'exploration : initiative ordonnée par la Vitesse,
    /// déplacement puis action, portée, zones d'effet, élévation.
    /// </summary>
    Tactical = 1,
}
