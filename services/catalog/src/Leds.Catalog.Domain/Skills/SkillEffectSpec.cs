namespace Leds.Catalog.Domain.Skills;

// One durable effect a skill applies to its targets. A skill may carry several
// (e.g. a heal-over-time AND a guard-over-time from the same cast) — Kind/Stat stay
// plain strings, the same cross-service-boundary convention used elsewhere
// (ConsequenceKind, ItemEquipmentEffectKind serialized as strings on the wire).
public sealed record SkillEffectSpec(
    string Kind,
    string? StatusKey,
    int Magnitude,
    int DurationTicks,
    int TickInterval = 0,
    string? Stat = null,
    bool MagnitudeIsPercentOfMax = false,
    // For StatModifier effects: when true, Magnitude is a percentage of the
    // combatant's BASE value for Stat (e.g. +10 = +10% AttackPower) rather than a
    // flat delta. This is now the default way to author stat buffs/debuffs — prefer
    // this over a flat Magnitude for new skills.
    bool MagnitudeIsPercentOfBaseStat = false,
    // When true, this effect is applied to the ACTOR casting the skill instead of
    // its targets — e.g. a damage skill that also buffs its own caster on hit.
    bool AppliesToActor = false,
    // When true, the effect never expires on its own — only removed by the target's
    // death (e.g. "Une destinée cruelle"). DurationTicks is ignored in this case.
    bool IsPermanent = false,
    string? AffinityRegister = null,
    string? AffinityOutcome = null,
    int AffinityPriority = 0);
