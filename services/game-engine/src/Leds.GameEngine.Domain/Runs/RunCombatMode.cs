namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// Le système de combat utilisé par une run, choisi à son lancement et fixe pour toute sa durée.
/// </summary>
/// <remarks>
/// <para>
/// Le jeu porte deux systèmes de combat qui coexistent indéfiniment (cf.
/// <c>docs/design/sfd-combat-trpg-v2.md</c>, §3). Ce choix ne ressuscite pas le
/// <c>RunExplorationMode</c> supprimé : l'exploration reste uniformément sur grille dans les deux
/// cas, et toute la couche hors-combat — nœuds, brouillard, fouille, récompenses, PNJ, marchands,
/// Lois, réputation, monnaie, Éclats — est rigoureusement identique. Seule la manière de jouer les
/// combats change.
/// </para>
/// <para>
/// Aucun changement de mode en cours de run n'est prévu : il faudrait transposer tout l'état de
/// combat et de position d'un système vers l'autre, sans bénéfice de conception.
/// </para>
/// </remarks>
public enum RunCombatMode
{
    /// <summary>
    /// Barre de temps continue : tempo vivant, coût d'investissement, momentum, rangs Front/Back.
    /// Mode historique, et défaut d'une run dont le mode n'a pas été précisé.
    /// </summary>
    Atb = 0,

    /// <summary>
    /// Tactique tour par tour sur la grille d'exploration : initiative ordonnée par la Vitesse,
    /// déplacement puis action, portée, zones d'effet, élévation.
    /// </summary>
    Tactical = 1,
}
