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
}
