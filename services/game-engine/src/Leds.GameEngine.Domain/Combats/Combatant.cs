using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed class Combatant
{
    private Combatant(
        CombatantId id,
        string sourceKey,
        string displayName,
        CombatantSide side,
        string archetype,
        int maxVitality,
        int currentVitality,
        int guard,
        int baseGuard,
        int mana,
        int charge,
        CombatantStatus status,
        IReadOnlyCollection<CombatantSkill> skills,
        CombatantBaseStatSnapshot baseStatSnapshot,
        CombatantRuntimeState runtimeState)
    {
        Id = id;
        SourceKey = sourceKey;
        DisplayName = displayName;
        Side = side;
        Archetype = archetype;
        MaxVitality = maxVitality;
        CurrentVitality = currentVitality;
        Guard = guard;
        BaseGuard = baseGuard;
        Mana = mana;
        Charge = charge;
        Status = status;
        Skills = skills;
        BaseStatSnapshot = baseStatSnapshot;
        RuntimeState = runtimeState;
    }

    public CombatantId Id { get; }
    public string SourceKey { get; }
    public string DisplayName { get; }
    public CombatantSide Side { get; }
    public string Archetype { get; }
    public int MaxVitality { get; }
    public int CurrentVitality { get; private set; }
    public int Guard { get; private set; }

    /// <summary>
    /// The passive guard floor restored at the start of each new round.
    /// Set from StartingGuardBonus run modifiers at combat creation.
    /// </summary>
    public int BaseGuard { get; private set; }

    public int Mana { get; private set; }
    public int Charge { get; private set; }
    public CombatantStatus Status { get; private set; }
    public IReadOnlyCollection<CombatantSkill> Skills { get; }

    public bool IsDefeated => Status == CombatantStatus.Defeated;
    /// <summary>
    /// Optional emotional attack type override, set from an AttackTypeOverride run
    /// modifier at combat creation (e.g. an item that changes the hero's type).
    /// When null, the combatant's attack type comes from its default type profile.
    /// </summary>
    public EmotionalType? AttackTypeOverride { get; private set; }

    /// <summary>
    /// Sets (or clears) the emotional attack type override. Applied at combat
    /// creation and restored on rehydration; never mutated mid-turn.
    /// </summary>
    public void ApplyAttackTypeOverride(EmotionalType? attackType)
    {
        AttackTypeOverride = attackType;
    }

    // ── ATB (Active Time Battle) ──────────────────────────────────────────────

    /// <summary>Current ATB gauge (0 = empty). Sourced from runtime state (persisted).</summary>
    public int AtbGauge => RuntimeState.AtbGaugeValue ?? 0;

    /// <summary>Absolute tick before which this combatant cannot fill its gauge (post-action recovery).</summary>
    public int AtbRecoveryUntilTick => RuntimeState.ActionRecoveryUntilTick ?? 0;

    /// <summary>
    /// Effective gauge gained per tick (Markov tempo, baked at combat preparation).
    /// Sourced from runtime state (persisted); defaults to a neutral 10 until baked.
    /// </summary>
    public int AtbFillPerTick => RuntimeState.AtbFillPerTick ?? 10;

    public void SetAtbFillPerTick(int fillPerTick) => RuntimeState.SetAtbFillPerTick(fillPerTick);

    public void SetAtbGauge(int value) => RuntimeState.SetAtbGauge(value);

    // ── Threat (enemy targeting) ────────────────────────────────────────────

    /// <summary>Accumulated threat this combatant has drawn over the fight so far.</summary>
    public double ThreatValue => RuntimeState.ThreatValue;

    /// <summary>Id of whoever most recently damaged this combatant, if any.</summary>
    public Guid? LastAttackerId => RuntimeState.LastAttackerId;

    public void AccrueThreat(double amount) => RuntimeState.AccrueThreat(amount);

    public void RecordLastAttacker(Guid attackerId) => RuntimeState.RecordLastAttacker(attackerId);

    /// <summary>
    /// Records that this combatant just acted: its gauge is consumed and it enters
    /// recovery until <paramref name="currentTick"/> + <paramref name="recoveryTicks"/>.
    /// </summary>
    public void RegisterAtbAction(int currentTick, int recoveryTicks)
    {
        RuntimeState.SetAtbGauge(0);
        RuntimeState.SetActionRecovery(recoveryTicks > 0 ? currentTick + recoveryTicks : null);
    }

    /// <summary>
    /// Immutable stat values frozen at combat creation.
    /// Canonical source for base/max/starting values.
    /// </summary>
    public CombatantBaseStatSnapshot BaseStatSnapshot { get; }

    /// <summary>
    /// Mutable runtime state during combat.
    /// Canonical source for current vitality, guard, mana, charge.
    /// </summary>
    public CombatantRuntimeState RuntimeState { get; }

    public static Combatant CreateAlly(
        string sourceKey,
        string displayName,
        string archetype,
        int maxVitality,
        int baseGuard = 0,
        IReadOnlyCollection<CombatantSkill>? skills = null)
    {
        var id = CombatantId.New();
        var snapshot = CombatantBaseStatSnapshot.Create(
            maxVitality: maxVitality,
            attackPower: 0,
            defense: 0,
            startingGuard: baseGuard,
            speed: 10,
            initiative: 0,
            recovery: 0,
            focus: 0,
            mana: 0,
            charge: 0);

        var runtimeState = CombatantRuntimeState.Create(
            currentVitality: maxVitality,
            currentGuard: baseGuard);

        return new Combatant(
            id,
            sourceKey,
            displayName,
            CombatantSide.Player,
            archetype,
            maxVitality,
            currentVitality: maxVitality,
            guard: baseGuard,
            baseGuard: baseGuard,
            mana: 0,
            charge: 0,
            CombatantStatus.Active,
            skills?.ToArray() ?? Array.Empty<CombatantSkill>(),
            snapshot,
            runtimeState);
    }

    public static Combatant CreateEnemy(
        string sourceKey,
        string displayName,
        string archetype,
        int maxVitality,
        IReadOnlyCollection<CombatantSkill>? skills = null,
        int startingGuard = 0,
        int attackPower = 0,
        int defense = 0,
        int speed = 10)
    {
        var id = CombatantId.New();
        var snapshot = CombatantBaseStatSnapshot.Create(
            maxVitality: maxVitality,
            attackPower: attackPower,
            defense: defense,
            startingGuard: startingGuard,
            speed: speed,
            initiative: 0,
            recovery: 0,
            focus: 0,
            mana: 0,
            charge: 0);

        var runtimeState = CombatantRuntimeState.Create(
            currentVitality: maxVitality,
            currentGuard: startingGuard);

        return new Combatant(
            id,
            sourceKey,
            displayName,
            CombatantSide.Enemy,
            archetype,
            maxVitality,
            currentVitality: maxVitality,
            guard: startingGuard,
            baseGuard: startingGuard,
            mana: 0,
            charge: 0,
            CombatantStatus.Active,
            skills?.ToArray() ?? Array.Empty<CombatantSkill>(),
            snapshot,
            runtimeState);
    }

    public static Combatant Create(
        CombatantId id,
        string sourceKey,
        string displayName,
        CombatantSide side,
        string archetype,
        int maxVitality,
        int currentVitality,
        int guard,
        int baseGuard,
        int mana,
        int charge,
        IReadOnlyCollection<CombatantSkill>? skills = null,
        int attackPower = 0,
        int defense = 0,
        int speed = 10, 
        int focus = 0)
    {
        if (id.Value == Guid.Empty)
            throw new DomainException("Combatant id is required.");

        if (string.IsNullOrWhiteSpace(sourceKey))
            throw new DomainException("Combatant source key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Combatant display name is required.");

        if (maxVitality <= 0)
            throw new DomainException("Combatant max vitality must be greater than zero.");

        if (currentVitality < 0 || currentVitality > maxVitality)
            throw new DomainException("Combatant current vitality must be between zero and max vitality.");

        if (guard < 0)
            throw new DomainException("Combatant guard must be non-negative.");

        if (baseGuard < 0)
            throw new DomainException("Combatant base guard must be non-negative.");

        if (mana < 0)
            throw new DomainException("Combatant mana must be non-negative.");

        if (charge < 0)
            throw new DomainException("Combatant charge must be non-negative.");

        var snapshot = CombatantBaseStatSnapshot.Create(
            maxVitality: maxVitality,
            attackPower: attackPower,
            defense: defense,
            startingGuard: baseGuard,
            speed: speed,
            initiative: 0,
            recovery: 0,
            focus: focus,
            mana: mana,
            charge: charge);

        var runtimeState = CombatantRuntimeState.Create(
            currentVitality: currentVitality,
            currentGuard: guard,
            currentMana: mana,
            currentCharge: charge);

        return new Combatant(
            id,
            sourceKey.Trim(),
            displayName.Trim(),
            side,
            archetype,
            maxVitality,
            currentVitality,
            guard,
            baseGuard,
            mana,
            charge,
            CombatantStatus.Active,
            skills?.ToArray() ?? Array.Empty<CombatantSkill>(),
            snapshot,
            runtimeState);
    }

    public void MarkDefeated()
    {
        if (Status == CombatantStatus.Defeated)
            throw new DomainException("Combatant is already defeated.");

        Status = CombatantStatus.Defeated;
        CurrentVitality = 0;
        RuntimeState.MarkDefeated();
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Damage amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatants cannot receive damage.");

        RuntimeState.ApplyDamage(amount);

        Guard = RuntimeState.CurrentGuard;
        CurrentVitality = RuntimeState.CurrentVitality;

        if (CurrentVitality == 0)
        {
            Status = CombatantStatus.Defeated;
        }
    }

    /// <summary>Direct vitality damage bypassing guard — used by DoT (poison/burn).</summary>
    public void ApplyVitalityDamage(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Damage amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatants cannot receive damage.");

        RuntimeState.ApplyVitalityDamage(amount);
        CurrentVitality = RuntimeState.CurrentVitality;

        if (CurrentVitality == 0)
        {
            Status = CombatantStatus.Defeated;
        }
    }

    public void GainGuard(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Guard amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatants cannot gain guard.");

        RuntimeState.GainGuard(amount);
        Guard = RuntimeState.CurrentGuard;
    }

    /// <summary>
    /// Restores Guard to BaseGuard at the start of each new round.
    /// Guard may still exceed BaseGuard from GainGuard (skill.basic.guard);
    /// we only restore if Guard has been consumed below BaseGuard.
    /// </summary>
    public void ResetGuardToBase()
    {
        if (IsDefeated) return;

        RuntimeState.ResetGuardToBase(BaseGuard);
        Guard = RuntimeState.CurrentGuard;
    }

    /// <summary>
    /// Rehydrates a combatant from a trusted persistence snapshot.
    /// This method must not be used to create a new gameplay combatant.
    /// </summary>
    public static Combatant Rehydrate(
        CombatantId id,
        string sourceKey,
        string displayName,
        CombatantSide side,
        string archetype,
        int maxVitality,
        int currentVitality,
        int guard,
        int baseGuard,
        int mana,
        int charge,
        CombatantStatus status,
        IReadOnlyCollection<CombatantSkill> skills,
        CombatantBaseStatSnapshot? baseStatSnapshot = null,
        CombatantRuntimeState? runtimeState = null,
        EmotionalType? attackTypeOverride = null)
    {
        var snapshot = baseStatSnapshot ?? CombatantBaseStatSnapshot.Rehydrate(
            Guid.NewGuid(),
            maxVitality,
            0,
            0,
            baseGuard,
            10,
            0,
            0,
            0,
            mana,
            charge,
            null,
            DateTime.UtcNow);

        var state = runtimeState ?? CombatantRuntimeState.Rehydrate(
            Guid.NewGuid(),
            currentVitality,
            guard,
            0,
            mana,
            charge,
            null,
            null,
            DateTime.UtcNow);

        var combatant = new Combatant(id, sourceKey, displayName, side, archetype, maxVitality, currentVitality, guard, baseGuard, mana, charge, status, skills, snapshot, state);
        combatant.AttackTypeOverride = attackTypeOverride;
        return combatant;
    }

    public void ApplyHeal(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Heal amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatant cannot be healed.");

        RuntimeState.ApplyHeal(MaxVitality, amount);
        CurrentVitality = RuntimeState.CurrentVitality;
    }

    public void DebugSetVitals(int vitality, int guard)
    {
        RuntimeState.DebugSetVitals(MaxVitality, vitality, guard);
        CurrentVitality = RuntimeState.CurrentVitality;
        Guard = RuntimeState.CurrentGuard;
        Status = CurrentVitality == 0
            ? CombatantStatus.Defeated
            : CombatantStatus.Active;
    }

    public void GainMana(int amount)
    {
        if (amount < 0)
            throw new DomainException("Mana gain amount cannot be negative.");

        Mana += amount;
        RuntimeState.GainMana(amount);
    }

    public void GainCharge(int amount)
    {
        if (amount < 0)
            throw new DomainException("Charge gain amount cannot be negative.");

        Charge += amount;
        RuntimeState.GainCharge(amount);
    }

    public void SpendMana(int amount)
    {
        if (amount <= 0)
            return;

        Mana = Math.Max(0, Mana - amount);
        RuntimeState.SpendMana(amount);
    }

    public void SpendCharge(int amount)
    {
        if (amount <= 0)
            return;

        Charge = Math.Max(0, Charge - amount);
        RuntimeState.SpendCharge(amount);
    }

    // ── Durable status effects (DoT/HoT, buffs/debuffs, control) ──────────────

    private readonly List<CombatStatusEffect> _statusEffects = new();

    /// <summary>Active durable effects (poison, regen, buffs/debuffs, stun…).</summary>
    public IReadOnlyCollection<CombatStatusEffect> StatusEffects => _statusEffects.AsReadOnly();

    /// <summary>
    /// Applies a status effect. Re-applying the same key refreshes duration and
    /// adds stacks; a fresh key is added. No-op on a defeated combatant.
    /// </summary>
    public void ApplyStatusEffect(CombatStatusEffect effect)
    {
        if (effect is null)
            throw new DomainException("Status effect is required.");

        if (IsDefeated)
            return;

        var existing = _statusEffects.FirstOrDefault(
            e => string.Equals(e.Key, effect.Key, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            existing.Reinforce(effect.Stacks, effect.ExpiresAtTick);
        else
            _statusEffects.Add(effect);
    }

    /// <summary>Rehydration only: re-attaches a persisted effect without stacking.</summary>
    public void RehydrateStatusEffect(CombatStatusEffect effect) => _statusEffects.Add(effect);

    /// <summary>
    /// Advances all durable effects to <paramref name="currentTick"/>: applies any due
    /// DoT/HoT and removes expired effects. Returns what happened (for combat logs).
    /// </summary>
    public IReadOnlyCollection<StatusTickEvent> TickStatusEffects(int currentTick)
    {
        if (_statusEffects.Count == 0)
            return Array.Empty<StatusTickEvent>();

        var events = new List<StatusTickEvent>();

        foreach (var effect in _statusEffects.ToArray())
        {
            if (!IsDefeated && effect.IsPeriodic)
            {
                var amount = effect.ConsumeDueTicks(currentTick, MaxVitality);
                if (amount > 0)
                {
                    if (effect.Kind == StatusEffectKind.DamageOverTime)
                    {
                        ApplyVitalityDamage(amount); // DoT bypasses guard
                        events.Add(new StatusTickEvent(effect.Key, effect.DisplayName, effect.Kind, amount, false));
                    }
                    else if (effect.Kind == StatusEffectKind.HealOverTime && CurrentVitality < MaxVitality)
                    {
                        ApplyHeal(amount);
                        events.Add(new StatusTickEvent(effect.Key, effect.DisplayName, effect.Kind, amount, false));
                    }
                    else if (effect.Kind == StatusEffectKind.GuardOverTime)
                    {
                        GainGuard(amount);
                        events.Add(new StatusTickEvent(effect.Key, effect.DisplayName, effect.Kind, amount, false));
                    }
                }
            }

            if (effect.IsExpired(currentTick))
            {
                _statusEffects.Remove(effect);
                events.Add(new StatusTickEvent(effect.Key, effect.DisplayName, effect.Kind, 0, true));
            }
        }

        return events;
    }

    private int StatModifierSum(CombatStat stat)
        => _statusEffects
            .Where(e => e.Kind == StatusEffectKind.StatModifier && e.Stat == stat)
            .Sum(e => e.Magnitude * e.Stacks);

    // Effective stats = base snapshot + active StatModifier effects (debuffs are negative).
    public int EffectiveAttackPower => Math.Max(0, BaseStatSnapshot.AttackPower + StatModifierSum(CombatStat.AttackPower));
    public int EffectiveDefense => Math.Max(0, BaseStatSnapshot.Defense + StatModifierSum(CombatStat.Defense));
    public int EffectiveSpeed => Math.Max(1, BaseStatSnapshot.Speed + StatModifierSum(CombatStat.Speed));
    public int EffectiveFocus => Math.Max(0, BaseStatSnapshot.Focus + StatModifierSum(CombatStat.Focus));

    // Control flags consumed by the ATB scheduler / action validation (tranche 3+).
    public bool IsStunned => _statusEffects.Any(e => e.Kind == StatusEffectKind.Stun);
    public bool IsSilenced => _statusEffects.Any(e => e.Kind == StatusEffectKind.Silence);
    public bool IsAtbLocked => IsStunned || _statusEffects.Any(e => e.Kind == StatusEffectKind.AtbLock);
}
