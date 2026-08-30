using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// Ce qu'un moteur de combat doit fournir pour que le noyau de résolution puisse travailler —
/// « ce combattant utilise cette compétence sur ces cibles, que se passe-t-il ».
/// </summary>
public interface ICombatContext
{
    CombatId Id { get; }

    /// <summary>
    /// Temps courant, en ticks. Les deux systèmes partagent cette unité : les durées de statut et
    /// de DoT sont authorées une seule fois, et la machinerie d'expiration
    /// (<see cref="Combatant.TickStatusEffects"/>) sert les deux. Un moteur tour par tour avance
    /// simplement d'un tour entier à la fois.
    /// </summary>
    /// <summary>
    /// Actif, gagné ou perdu. Exposé ici parce que la clôture d'un combat — récompense,
    /// journal, sortie de salle — est une affaire de run, identique quel que soit le système
    /// qui a produit l'issue.
    /// </summary>
    CombatStatus Status { get; }

    int CurrentTick { get; }

    /// <summary>Numéro de tour courant. Entre dans la graine des tirages déterministes.</summary>
    int TurnNumber { get; }

    IReadOnlyCollection<Combatant> Allies { get; }
    IReadOnlyCollection<Combatant> Enemies { get; }

    EmotionalAffinityMatrixSnapshot EmotionalAffinityMatrix { get; }

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

}
