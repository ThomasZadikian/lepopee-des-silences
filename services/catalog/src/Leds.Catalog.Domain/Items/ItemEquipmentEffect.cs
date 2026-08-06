using Leds.Catalog.Domain.Npcs;
using Leds.Catalog.Domain.Gameplay;

namespace Leds.Catalog.Domain.Items;

// One effect an equipped item produces, only for as long as it stays equipped (reversible —
// SFD "Système d'équipement et sac permanent" § 8). StatKind mirrors the player-service
// PlayerStatKind names as a plain string (e.g. "AttackPower") — catalog doesn't reference
// player-service's enum directly, same cross-service-boundary convention used elsewhere
// (ConsequenceKind, EmotionalRegister serialized as strings on the wire).
public sealed record ItemEquipmentEffect(
    ItemEquipmentEffectKind Kind,
    string? StatKind = null,
    int? Amount = null,
    string? SkillKey = null,
    EmotionalRegister? AffinityRegister = null,
    // "key:value" condition gating this effect (e.g. "room:Montagne", "weather:Accalmie") —
    // null means always-on while equipped. Only StatBonus/StatBonusPercent kinds currently
    // support a condition; game-engine re-evaluates it fresh every combat instead of baking
    // it into the character's static run-start stats (see PlayerStatMerger/CombatFactory).
    string? Condition = null,
    AffinityOutcome? AffinityOutcome = null,
    int Priority = 0,
    int? DurationActivations = null,
    string? BehaviorCode = null);
