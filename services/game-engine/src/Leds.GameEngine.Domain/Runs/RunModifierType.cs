namespace Leds.GameEngine.Domain.Runs;

public enum RunModifierType
{
    StartingGuardBonus = 0,
    NextCombatDifficultyMultiplier = 1,
    RewardPowerMultiplierBonus = 2,
    PermanentCombatDifficultyBonus = 3,
    CombatDifficultyMultiplier = 4,
    RewardPowerMultiplier = 5,
    AttackPowerBonus = 6,
    DefenseBonus = 7,
    SpeedBonus = 8,
    InitiativeBonus = 9,
    RecoveryBonus = 10,
    FocusBonus = 11,
    ManaBonus = 12,
    ChargeBonus = 13,
    RoomClimate = 14,
    AttackTypeOverride = 15,

    // Lois du Palais — mécaniques introduites par le Compendium des Lois (chapitres IV/VIII/IX).
    // Ajoutés en fin de liste uniquement : les valeurs sont sérialisées par nom (voir
    // RunModifierEntity.Type), mais on évite quand même de réordonner par prudence.
    TurnOrderLock = 16,
    TurnOrderReverse = 17,

    /// <summary>"Loi de la Curée" (law.curee) — +15% damage taken while below 25% max
    /// HP, symmetric across both sides. Renamed from the never-seeded "ExecuteThreshold"
    /// placeholder, which wrongly assumed an instant-kill mechanic (safe to rename in
    /// place — never persisted, and RunModifierEntity.Type is stored by name anyway).</summary>
    DamageAmplificationBelowHpThreshold = 18,
    RoomTraversalHpDrain = 19,
    HitCounterDoubleDamage = 20,
    MirrorCombatCopy = 21,
    SuspendSevereLaws = 22,

    /// <summary>"Loi de la Première Impression" (law.premiere-impression) — the combat's
    /// first landed hit, any side, is forced critical. See Combat.FirstHitCriticalEnabled.</summary>
    FirstHitCritical = 23,

    /// <summary>"Loi de l'Écriture" (law.ecriture) — every DamageOverTime effect (both sides)
    /// lasts N extra turns. Value is the bonus turn count (converted to ticks at
    /// CombatFactory time via AtbConstants.TicksPerTurn), not a raw tick count.</summary>
    DotDurationExtension = 24,

    /// <summary>"Loi du Duel" (law.duel) — mono-target skills deal +20% damage, AoE skills
    /// deal -20%, both sides. See Combat.DuelDamageAsymmetryEnabled.</summary>
    DuelDamageAsymmetry = 25,

    /// <summary>"Loi de la Destinée" (law.destinee) — every combatant, both sides, receives
    /// the exact "Une destinée cruelle" bundle (canon.skill.destinee-cruelle) for the room:
    /// +20% Attack/Defense/Speed/Focus, -15% ATB tempo, and a 10%-max-HP DoT with no end.
    /// Applied once at CombatFactory time (like TurnOrderReverse) — no Combat-level flag
    /// needed since nothing has to be checked live during the fight.</summary>
    CruelDestinyForEveryone = 26,
}
