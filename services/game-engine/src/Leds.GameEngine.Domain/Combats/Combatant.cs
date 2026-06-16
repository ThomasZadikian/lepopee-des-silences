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
        IReadOnlyCollection<CombatantSkill>? skills = null)
    {
        var id = CombatantId.New();
        var snapshot = CombatantBaseStatSnapshot.Create(
            maxVitality: maxVitality,
            attackPower: 0,
            defense: 0,
            startingGuard: 0,
            speed: 10,
            initiative: 0,
            recovery: 0,
            focus: 0,
            mana: 0,
            charge: 0);

        var runtimeState = CombatantRuntimeState.Create(
            currentVitality: maxVitality,
            currentGuard: 0);

        return new Combatant(
            id,
            sourceKey,
            displayName,
            CombatantSide.Enemy,
            archetype,
            maxVitality,
            currentVitality: maxVitality,
            guard: 0,
            baseGuard: 0,
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
        int speed = 10)
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
            focus: 0,
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
        CombatantRuntimeState? runtimeState = null)
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

        return new Combatant(id, sourceKey, displayName, side, archetype, maxVitality, currentVitality, guard, baseGuard, mana, charge, status, skills, snapshot, state);
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
}