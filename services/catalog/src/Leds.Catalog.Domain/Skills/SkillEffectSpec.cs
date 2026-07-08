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
    bool MagnitudeIsPercentOfMax = false);
