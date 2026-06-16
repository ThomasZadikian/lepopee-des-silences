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
}
