using Leds.Catalog.Domain.Npcs;

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
    EmotionalRegister? AffinityRegister = null);
