using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats.StatusEffects;

/// <summary>
/// A durable effect on a combatant that advances with holder activations. Periodic effects
/// (DoT/HoT) apply <see cref="Magnitude"/> × <see cref="Stacks"/> every
/// <see cref="TickInterval"/> ticks; all effects end at <see cref="ExpiresAtTick"/>.
/// Stat modifiers and control effects have <see cref="TickInterval"/> = 0.
/// </summary>
public sealed class CombatStatusEffect
{
    private CombatStatusEffect(
        string key,
        string displayName,
        StatusEffectKind kind,
        EmotionalType? emotionalType,
        CombatStat stat,
        int magnitude,
        int stacks,
        int tickInterval,
        int nextTickAtTick,
        int expiresAtTick,
        bool isMagnitudePercentOfMax,
        bool isMagnitudePercentOfBaseStat,
        IReadOnlyCollection<CombatantSkill>? grantedSkills,
        bool isPermanent,
        IReadOnlyCollection<Guid?>? stackSourceIds)
    {
        Key = key;
        DisplayName = displayName;
        Kind = kind;
        EmotionalType = emotionalType;
        Stat = stat;
        Magnitude = magnitude;
        _stackSourceIds = stackSourceIds?.Take(5).ToList() ?? [];
        while (_stackSourceIds.Count < stacks)
            _stackSourceIds.Add(null);
        TickInterval = tickInterval;
        NextTickAtTick = nextTickAtTick;
        ExpiresAtTick = expiresAtTick;
        IsMagnitudePercentOfMax = isMagnitudePercentOfMax;
        IsMagnitudePercentOfBaseStat = isMagnitudePercentOfBaseStat;
        GrantedSkills = grantedSkills ?? Array.Empty<CombatantSkill>();
        IsPermanent = isPermanent;
    }
    private const int MinTickInterval = 1400;
    private readonly List<Guid?> _stackSourceIds;

    public string Key { get; }
    public string DisplayName { get; }
    public StatusEffectKind Kind { get; }
    public EmotionalType? EmotionalType { get; }
    public CombatStat Stat { get; }
    public int Magnitude { get; }
    public int Stacks => _stackSourceIds.Count;
    public IReadOnlyList<Guid?> StackSourceIds => _stackSourceIds.AsReadOnly();
    public Guid? FirstStackSourceId => _stackSourceIds.FirstOrDefault();
    public int TickInterval { get; }
    public int NextTickAtTick { get; private set; }
    public int ExpiresAtTick { get; private set; }
    /// <summary>When true (any periodic kind — HealOverTime, or DamageOverTime for a
    /// max-HP-scaled curse like "Une destinée cruelle"), Magnitude is a percentage of the
    /// target's MaxVitality rather than a flat amount — see <see cref="ConsumeDueTicks"/>.</summary>
    public bool IsMagnitudePercentOfMax { get; }

    /// <summary>When true (StatModifier only), Magnitude is a percentage of the target's
    /// BASE value for <see cref="Stat"/> rather than a flat delta — this is now the
    /// default way to author stat buffs/debuffs. See <see cref="Combatant"/>'s
    /// Effective* properties for how this is combined with flat modifiers.</summary>
    public bool IsMagnitudePercentOfBaseStat { get; }

    /// <summary>When Kind is SkillGrant: the target's skill snapshot at cast time,
    /// temporarily usable by whoever holds this effect until it expires — see
    /// <see cref="Combatant.Skills"/> (Création, sort légendaire de l'Architecte).</summary>
    public IReadOnlyCollection<CombatantSkill> GrantedSkills { get; }

    /// <summary>
    /// When true, this effect never expires from <see cref="IsExpired"/> — it is only
    /// ever removed by the combatant's death (e.g. "Une destinée cruelle": a curse
    /// that lasts for the rest of the fight, at the cost of an ongoing % max-HP drain).
    /// <see cref="ExpiresAtTick"/> is set to <see cref="int.MaxValue"/> and effectively
    /// unused for a permanent effect.
    /// </summary>
    public bool IsPermanent { get; }

    public bool IsPeriodic => TickInterval > 0
        && (Kind is StatusEffectKind.DamageOverTime or StatusEffectKind.HealOverTime or StatusEffectKind.GuardOverTime);

    /// <summary>
    /// Creates a new effect anchored to the holder's status clock. <paramref name="durationTicks"/>
    /// is how long it lasts; <paramref name="tickInterval"/> = 0 for non-periodic effects.
    /// </summary>
    public static CombatStatusEffect Create(
        string key,
        string displayName,
        StatusEffectKind kind,
        int currentTick,
        int durationTicks,
        int magnitude = 0,
        int stacks = 1,
        int tickInterval = 0,
        CombatStat stat = CombatStat.None,
        EmotionalType? emotionalType = null,
        bool isMagnitudePercentOfMax = false,
        bool isMagnitudePercentOfBaseStat = false,
        IReadOnlyCollection<CombatantSkill>? grantedSkills = null,
        bool isPermanent = false,
        Guid? sourceCombatantId = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Status effect key is required.");
        if (!isPermanent && durationTicks <= 0)
            throw new DomainException("Status effect duration must be positive.");
        if (stacks is < 1 or > 5)
            throw new DomainException("Status effect stacks must be between one and five.");

        // Floor so nothing ticks faster than ~once every 2s, whatever the data says.
        var interval = tickInterval <= 0 ? 0 : Math.Max(tickInterval, MinTickInterval);
        var nextTick = interval > 0 ? currentTick + interval : int.MaxValue;
        var expiresAtTick = isPermanent ? int.MaxValue : currentTick + durationTicks;

        return new CombatStatusEffect(
            key.Trim(),
            string.IsNullOrWhiteSpace(displayName) ? key.Trim() : displayName.Trim(),
            kind,
            emotionalType,
            stat,
            magnitude,
            stacks,
            interval,
            nextTick,
            expiresAtTick,
            isMagnitudePercentOfMax,
            isMagnitudePercentOfBaseStat,
            grantedSkills,
            isPermanent,
            Enumerable.Repeat(sourceCombatantId, stacks).ToArray());
    }

    public static CombatStatusEffect Rehydrate(
        string key, string displayName, StatusEffectKind kind, EmotionalType? emotionalType,
        CombatStat stat, int magnitude, int stacks, int tickInterval, int nextTickAtTick, int expiresAtTick,
        bool isMagnitudePercentOfMax = false,
        bool isMagnitudePercentOfBaseStat = false,
        IReadOnlyCollection<CombatantSkill>? grantedSkills = null,
        bool isPermanent = false,
        IReadOnlyCollection<Guid?>? stackSourceIds = null)
        => new(key, displayName, kind, emotionalType, stat, magnitude, stacks, tickInterval, nextTickAtTick, expiresAtTick, isMagnitudePercentOfMax, isMagnitudePercentOfBaseStat, grantedSkills, isPermanent, stackSourceIds);

    public bool IsExpired(int currentTick) => !IsPermanent && currentTick >= ExpiresAtTick;

    /// <summary>
    /// Re-applying the same effect adds stacks (capped) — remaining duration is
    /// NEVER touched by stacking (e.g. 2 stacks of poison with 7 ticks left, re-cast
    /// a 3rd time, stays at 3 stacks / 7 ticks left). The only way to change remaining
    /// duration is <see cref="ExtendDuration"/>, an explicit skill/effect mechanic
    /// (e.g. l'Écrivain's "Écriture continuelle").
    /// </summary>
    public void Reinforce(CombatStatusEffect reinforcement, int maxStacks = 5)
    {
        ArgumentNullException.ThrowIfNull(reinforcement);
        var cappedMaximum = Math.Min(5, Math.Max(1, maxStacks));
        foreach (var sourceId in reinforcement.StackSourceIds)
        {
            if (_stackSourceIds.Count >= cappedMaximum)
                break;
            _stackSourceIds.Add(sourceId);
        }
    }

    public void Reinforce(int additionalStacks, int maxStacks = 5)
    {
        var cappedMaximum = Math.Min(5, Math.Max(1, maxStacks));
        for (var index = 0; index < Math.Max(0, additionalStacks); index++)
        {
            if (_stackSourceIds.Count >= cappedMaximum)
                break;
            _stackSourceIds.Add(null);
        }
    }

    public bool RemoveOneStack()
    {
        if (Stacks <= 1)
            return true;

        _stackSourceIds.RemoveAt(_stackSourceIds.Count - 1);
        return false;
    }

    /// <summary>
    /// Directly extends remaining duration to <paramref name="newExpiresAtTick"/> —
    /// the ONLY way a status effect's duration changes after creation (stacking via
    /// <see cref="Reinforce"/> never does). No-op if not later than the current expiry.
    /// </summary>
    public void ExtendDuration(int newExpiresAtTick)
    {
        if (newExpiresAtTick > ExpiresAtTick)
            ExpiresAtTick = newExpiresAtTick;
    }

    /// <summary>
    /// Read-only preview of the amount ONE tick will deal/heal/guard right now (no
    /// side effect on the tick schedule) — for UI display (e.g. a status tooltip
    /// showing "real damage per tick"), not for resolution (see <see cref="ConsumeDueTicks"/>
    /// for the actual, schedule-advancing computation, which this mirrors exactly).
    /// Returns 0 for non-periodic effects.
    /// </summary>
    public int PeekPerTickAmount(int maxVitalityForPercent = 0)
    {
        if (!IsPeriodic)
            return 0;

        var perTick = IsMagnitudePercentOfMax
            ? (int)Math.Round(maxVitalityForPercent * (Magnitude / 100.0))
            : Magnitude;

        return perTick * Stacks;
    }

    /// <summary>
    /// For periodic effects: returns the total magnitude due since the last tick up to
    /// <paramref name="currentTick"/> (and not past expiry), advancing the schedule.
    /// Returns 0 for non-periodic effects. When <see cref="IsMagnitudePercentOfMax"/> is
    /// set, each tick's amount is computed as a percentage of <paramref name="maxVitalityForPercent"/>
    /// instead of the flat <see cref="Magnitude"/>.
    /// </summary>
    public int ConsumeDueTicks(int currentTick, int maxVitalityForPercent = 0)
    {
        if (!IsPeriodic)
            return 0;

        var total = 0;
        while (NextTickAtTick <= currentTick && NextTickAtTick <= ExpiresAtTick)
        {
            var perTick = IsMagnitudePercentOfMax
                ? (int)Math.Round(maxVitalityForPercent * (Magnitude / 100.0))
                : Magnitude;
            total += perTick * Stacks;
            NextTickAtTick += TickInterval;
        }

        return total < 0 ? 0 : total;
    }
}
