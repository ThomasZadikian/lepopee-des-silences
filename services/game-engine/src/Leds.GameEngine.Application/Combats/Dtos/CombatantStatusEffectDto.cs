namespace Leds.GameEngine.Application.Combats.Dtos;

/// <summary>A durable status effect on a combatant, surfaced for the combat UI.</summary>
public sealed record CombatantStatusEffectDto(
    string Key,
    string DisplayName,
    // DamageOverTime | HealOverTime | GuardOverTime | StatModifier | Stun | Silence |
    // AtbLock | SkillGrant
    string Kind,
    string Stat,   // AttackPower | Defense | Speed | Focus | ... | None
    int Magnitude,
    int Stacks,
    // True when Magnitude is a percentage of BASE Stat (StatModifier) rather than a
    // flat delta — lets the UI show "±X%" only when that's what Magnitude means.
    bool IsMagnitudePercentOfBaseStat,
    // What ONE tick deals/heals/guards right now (already resolves %-of-max-HP and
    // stacks) — for a status tooltip showing "real damage per tick". 0 for non-periodic
    // effects (StatModifier/Stun/Silence/AtbLock/SkillGrant).
    int PerTickAmount,
    // Ticks remaining until this effect ends, from the combat's CurrentTick — null
    // when IsPermanent (never ends on its own).
    int? TicksRemaining,
    bool IsPermanent);
