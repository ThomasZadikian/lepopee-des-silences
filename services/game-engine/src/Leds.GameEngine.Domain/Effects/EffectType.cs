using System.Text.Json.Serialization;

namespace Leds.GameEngine.Domain.Effects;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectType
{
    HealVitality,
    DamageVitality,
    AddCurrentGuard,
    AddStartingGuard,
    ModifyAttackPower,
    ModifyDefense,
    ModifySpeed,
    ModifyInitiative,
    ModifyRecovery,
    ModifyDifficultyMultiplier,
    ModifyRewardPowerMultiplier,
    RestoreFocus,
    RestoreMana,
    RestoreCharge,
    ApplyWeaken,
    ApplyDisrupt,
    GrantRunItem,
    GrantRunModifier,
    GrantPermanentUnlockCandidate,
    ModifyEnemyBehavior,
    ModifyTargetingBias,
    ModifyGenerationWeight,
    ModifyRoomSelectionBias,
    ModifyEnemySelectionBias,
    ModifyRewardSelectionBias,
    ModifyLawSelectionBias,
    ModifyCurseSelectionBias,
    ApplyBehaviorTag,
    ApplyNarrativePressure,
    ApplyRoomClimate,

    // Lois du Palais — gate-style effects for the Compendium's exotic combat mechanics
    // (chapitres IV/VIII). The effect's Value only needs to be non-zero (PalaceLawEffect.Create
    // rejects zero); the mechanic reads the RunModifier's mere presence, not its magnitude.
    /// <summary>"Loi du Sablier Renversé" (law.sablier) → RunModifierType.TurnOrderReverse.</summary>
    EnableTurnOrderReverse,
    /// <summary>"Loi de la File Indienne" (law.file-indienne) → RunModifierType.TurnOrderLock.</summary>
    EnableTurnOrderLock,
    /// <summary>"Loi de la Dévoration" (law.devoration) → RunModifierType.RoomTraversalHpDrain
    /// (gates both the room-traversal drain and the per-win restore).</summary>
    EnableRoomTraversalHpDrain,
    /// <summary>"Loi du Treizième Coup" (law.treizieme-coup) → RunModifierType.HitCounterDoubleDamage.</summary>
    EnableHitCounterDoubleDamage,
    /// <summary>"Loi du Reflet" (law.reflet) → RunModifierType.MirrorCombatCopy.</summary>
    EnableMirrorCombatCopy,
    /// <summary>"Loi de la Première Impression" (law.premiere-impression) → RunModifierType.FirstHitCritical.</summary>
    EnableFirstHitCritical,
    /// <summary>"Loi de la Curée" (law.curee) → RunModifierType.DamageAmplificationBelowHpThreshold.</summary>
    EnableLowHpDamageAmplification,
    /// <summary>"Loi de l'Écriture" (law.ecriture) → RunModifierType.DotDurationExtension. Unlike
    /// the other gate-style effects above, the Value here is meaningful: the bonus turn count.</summary>
    EnableDotDurationExtension,
    /// <summary>"Loi du Duel" (law.duel) → RunModifierType.DuelDamageAsymmetry.</summary>
    EnableDuelDamageAsymmetry,
    /// <summary>"Loi de la Destinée" (law.destinee) → RunModifierType.CruelDestinyForEveryone.</summary>
    EnableCruelDestinyForEveryone,
    /// <summary>"Édit du Souvenir Doux" (law.souvenir-doux) → RunModifierType.AllyHealingBonus.</summary>
    EnableAllyHealingBonus,
    /// <summary>"Loi du Nom Retenu" (law.nom-retenu) → RunModifierType.ReputationChangeDoubled.</summary>
    EnableReputationChangeDoubled,
    /// <summary>"Loi du Témoin" (law.temoin) → RunModifierType.WoundHealingBlocked.</summary>
    EnableWoundHealingBlocked,
    /// <summary>"Loi du Silence Dû" (law.silence-du) → RunModifierType.SilenceDuActive.</summary>
    EnableSilenceDuActive,
}
