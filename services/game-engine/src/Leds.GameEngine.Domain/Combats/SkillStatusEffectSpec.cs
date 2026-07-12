using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// The durable status effect a skill applies to its targets (poison, regen,
/// buff/debuff, stun…). Null on skills that only deal instant damage/heal/guard.
/// Durations and intervals are in ATB ticks.
/// </summary>
public sealed record SkillStatusEffectSpec(
    string Key,
    string DisplayName,
    StatusEffectKind Kind,
    int Magnitude,
    int DurationTicks,
    int TickInterval = 0,
    CombatStat Stat = CombatStat.None,
    EmotionalType? EmotionalType = null,
    int Stacks = 1,
    bool MagnitudeIsPercentOfMax = false,
    bool MagnitudeIsPercentOfBaseStat = false,
    // When true, applied to the ACTOR casting the skill instead of its targets.
    bool AppliesToActor = false,
    // When true, the effect never expires on its own — only removed by death (e.g.
    // "Une destinée cruelle"). DurationTicks is ignored in this case.
    bool IsPermanent = false);
