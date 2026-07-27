namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// Ce qu'un moteur de combat doit fournir pour que le noyau de résolution puisse travailler —
/// « ce combattant utilise cette compétence sur ces cibles, que se passe-t-il ».
/// </summary>
/// <remarks>
/// <para>
/// Le jeu porte deux systèmes de combat qui coexistent (cf. <c>docs/design/sfd-combat-trpg-v2.md</c>,
/// §2) : l'ATB à barre de temps continue, et le tactique tour par tour sur grille. Le principe
/// directeur est qu'ils sont <b>indépendants du déroulé</b> — ordonnancement, modèle spatial,
/// économie d'action — mais <b>partagent la résolution</b> : dégâts physiques et magiques, statuts,
/// DoT, garde continue, soins, résistances typées, critiques et échecs.
/// </para>
/// <para>
/// Appliquer l'indépendance à la résolution reviendrait à dupliquer la formule de dégâts et le
/// Bestiaire ; deux copies qui divergeraient au premier correctif. Cette interface est la couture :
/// le noyau de résolution ne connaît que ce contrat, jamais un agrégat concret. <see cref="Combat"/>
/// l'implémente pour l'ATB ; l'agrégat tactique l'implémentera à son tour, en frère et non en
/// héritier.
/// </para>
/// <para>
/// <b>Règle d'ajout</b> : n'exposer ici que ce dont la <i>résolution</i> a besoin. Tout ce qui
/// relève de l'ordonnancement (jauges, tempo, momentum, initiative, ordre de tour) reste privé à
/// chaque agrégat, à l'exception des deux crochets explicitement neutralisables en fin
/// d'interface.
/// </para>
/// </remarks>
public interface ICombatContext
{
    CombatId Id { get; }

    /// <summary>
    /// Temps courant, en ticks. Les deux systèmes partagent cette unité : les durées de statut et
    /// de DoT sont authorées une seule fois, et la machinerie d'expiration
    /// (<see cref="Combatant.TickStatusEffects"/>) sert les deux. Un moteur tour par tour avance
    /// simplement d'un tour entier à la fois.
    /// </summary>
    int CurrentTick { get; }

    /// <summary>Numéro de tour courant. Entre dans la graine des tirages déterministes.</summary>
    int TurnNumber { get; }

    IReadOnlyCollection<Combatant> Allies { get; }
    IReadOnlyCollection<Combatant> Enemies { get; }

    // ── Lois du Palais actives sur ce combat ────────────────────────────────────────────────
    // Figées à la création depuis les RunModifiers actifs. Elles modifient la résolution, pas
    // l'ordonnancement : elles valent donc pour les deux systèmes.

    /// <summary>« Loi de la Curée » : +15% de dégâts subis sous 25% de PV max.</summary>
    bool LowHpDamageAmplificationEnabled { get; }

    /// <summary>« Loi du Silence des Soins » : tout soin est annulé.</summary>
    bool HealingBlocked { get; }

    /// <summary>« Loi du Duel » : bonus en mono-cible, malus en zone.</summary>
    bool DuelDamageAsymmetryEnabled { get; }

    /// <summary>« Loi de la Dévoration » : dégâts par tour ajoutés à chaque DoT posé.</summary>
    int DotMagnitudeBonus { get; }

    /// <summary>« Loi de la Rémanence » : ticks ajoutés à la durée de chaque DoT posé.</summary>
    int DotDurationExtensionTicks { get; }

    /// <summary>
    /// « Loi du Treizième Coup » : enregistre un coup porté et renvoie <c>true</c> quand il s'agit
    /// du treizième (puis tous les treize), tous camps confondus. L'appelant double alors les
    /// dégâts.
    /// </summary>
    bool RegisterLandedHit();

    /// <summary>
    /// « Loi de la Première Impression » : renvoie <c>true</c> exactement une fois par combat, au
    /// tout premier coup porté, quel qu'en soit l'auteur.
    /// </summary>
    bool TryConsumeFirstHitCritical();

    /// <summary>« Loi de l'Éloge Funèbre » : arme la restriction d'attaque basique du prochain
    /// agissant.</summary>
    void RegisterCombatantDefeated();

    /// <summary>« Loi de la Troisième Tasse » : peut réduire de moitié un soin et empoisonner sa
    /// cible. Sans effet quand la loi est inactive.</summary>
    (int HealAmount, bool Triggered) ApplyThirdCupRollIfActive(Combatant target, int healAmount);

    // ── Crochets d'ordonnancement ───────────────────────────────────────────────────────────
    // La résolution les déclenche, mais leur sens dépend du moteur. L'ATB les honore ; un moteur
    // tour par tour les neutralise sans que le noyau ait à le savoir.

    /// <summary>
    /// Une frappe interrompt la cible. En ATB, repousse sa jauge (davantage si elle canalisait).
    /// </summary>
    void InterruptAction(Guid targetId);

    /// <summary>
    /// Un geste marquant accélère la reprise de main de son auteur. Propre au tempo ATB : sans
    /// objet là où l'ordre de tour est fixe.
    /// </summary>
    void AwardTempoMomentum(Combatant combatant, int amountPerMille);
}
