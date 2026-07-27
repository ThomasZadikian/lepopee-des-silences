namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>
/// À quelle distance une compétence porte, sur une grille.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ TABLE AUTHORÉE, DÉRIVÉE FAUTE DE MIEUX. Le catalogue de compétences a été écrit pour l'ATB,
/// où la portée n'existe pas : aucune compétence ne déclare la sienne. Plutôt que d'attendre
/// que les ~138 sorts soient re-authorés, la portée est déduite de ce qu'ils disent déjà —
/// leur catégorie et leur type.
/// </para>
/// <para>
/// La lecture est volontairement grossière mais lisible en jeu : ce qui frappe avec le corps
/// frappe au contact, ce qui frappe avec l'esprit frappe de loin, ce qui soutient se tient à
/// mi-distance. Le jour où une compétence déclarera sa propre portée, elle prendra le pas sur
/// cette table sans que rien d'autre ne bouge.
/// </para>
/// </remarks>
public static class TacticalRange
{
    /// <summary>Portée au contact : la case adjacente. // BALANCE KNOB</summary>
    public const int Melee = 1;

    /// <summary>Portée de soutien : assez pour couvrir sa ligne sans la rejoindre. // BALANCE KNOB</summary>
    public const int Support = 3;

    /// <summary>Portée magique. // BALANCE KNOB</summary>
    public const int Ranged = 4;

    /// <summary>
    /// La portée de <paramref name="skill"/>, et si elle exige une ligne de vue dégagée.
    /// </summary>
    /// <remarks>
    /// Une frappe au contact ne demande pas de ligne de vue : à bout portant, la question ne se
    /// pose pas. Tout ce qui porte plus loin doit voir sa cible — c'est ce qui donne aux crêtes
    /// et aux éboulis leur valeur défensive.
    /// </remarks>
    public static (int Range, bool RequiresLineOfSight) For(CombatantSkill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        // Le soutien d'abord : un soin magique doit se lire comme un soin, pas comme un sort
        // offensif à longue portée.
        if (skill.SkillType is "Heal" or "Defense")
            return (Support, true);

        return string.Equals(skill.Category, "Magic", StringComparison.OrdinalIgnoreCase)
            ? (Ranged, true)
            : (Melee, false);
    }
}
