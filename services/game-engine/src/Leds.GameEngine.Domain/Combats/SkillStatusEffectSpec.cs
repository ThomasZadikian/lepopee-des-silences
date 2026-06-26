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
    int Stacks = 1);
