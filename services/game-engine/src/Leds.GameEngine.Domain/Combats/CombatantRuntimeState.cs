using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed class CombatantRuntimeState
{
    private CombatantRuntimeState(
        Guid id,
        int currentVitality,
        int currentGuard,
        int currentFocus,
        int currentMana,
        decimal currentCharge,
        DateTime updatedAtUtc,
        double threatValue,
        Guid? lastAttackerId,
        int maxMana,
        bool tookPowerfulHitSinceLastAction)
    {
        Id = id;
        CurrentVitality = currentVitality;
        CurrentGuard = currentGuard;
        CurrentFocus = currentFocus;
        CurrentMana = currentMana;
        MaxMana = maxMana;
        CurrentCharge = currentCharge;
        UpdatedAtUtc = updatedAtUtc;
        ThreatValue = threatValue;
        LastAttackerId = lastAttackerId;
        TookPowerfulHitSinceLastAction = tookPowerfulHitSinceLastAction;
    }

    public Guid Id { get; }
    public int CurrentVitality { get; private set; }
    public int CurrentGuard { get; private set; }
    public int CurrentFocus { get; private set; }
    public int CurrentMana { get; private set; }
    public int MaxMana { get; }
    public decimal CurrentCharge { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Accumulated threat this combatant has drawn over the course of the fight
    /// (damage dealt, healing, guard gained — see ThreatTuning). Meaningful on
    /// the player side; enemies weigh candidate targets by this value.
    /// </summary>
    public double ThreatValue { get; private set; }

    /// <summary>Id of the combatant who most recently damaged this one, if any.</summary>
    public Guid? LastAttackerId { get; private set; }

    /// <summary>
    /// True if this combatant took a single hit for ≥25% of its MaxVitality since its
    /// last action — the Bestiaire's uniform "coup très puissant" threshold (source:
    /// SFD Bestiaire du Palais, §I). Set by <see cref="RecordDamageTaken"/>, read via
    /// the one-shot <see cref="ConsumePowerfulHitSinceLastAction"/> so a scripted
    /// reaction only fires once per qualifying hit.
    /// </summary>
    public bool TookPowerfulHitSinceLastAction { get; private set; }

    public bool IsDefeated => CurrentVitality <= 0;

    public static CombatantRuntimeState Create(
        int currentVitality,
        int currentGuard,
        int currentFocus = 0,
        int currentMana = 0,
        decimal currentCharge = 0,
        double threatValue = 0,
        Guid? lastAttackerId = null,
        int? maxMana = null,
        bool tookPowerfulHitSinceLastAction = false)
    {
        if (currentVitality < 0)
            throw new DomainException("Current vitality cannot be negative.");

        if (currentGuard < 0)
            throw new DomainException("Current guard cannot be negative.");

        if (currentFocus < 0)
            throw new DomainException("Current focus cannot be negative.");

        if (currentMana < 0)
            throw new DomainException("Current mana cannot be negative.");

        if (currentCharge < 0)
            throw new DomainException("Current charge cannot be negative.");

        var resolvedMaxMana = maxMana ?? int.MaxValue;

        if (currentMana > resolvedMaxMana)
            throw new DomainException("Current mana cannot exceed max mana.");

        return new CombatantRuntimeState(
            Guid.NewGuid(),
            currentVitality,
            currentGuard,
            currentFocus,
            currentMana,
            currentCharge,
            DateTime.UtcNow,
            threatValue,
            lastAttackerId,
            resolvedMaxMana,
            tookPowerfulHitSinceLastAction);
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Damage amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatants cannot receive damage.");

        var remaining = amount;

        if (CurrentGuard > 0)
        {
            var absorbed = Math.Min(CurrentGuard, remaining);
            CurrentGuard -= absorbed;
            remaining -= absorbed;
        }

        if (remaining > 0)
        {
            CurrentVitality = Math.Max(0, CurrentVitality - remaining);
        }

        Touch();
    }

    /// <summary>Direct vitality damage that bypasses guard (poison/burn DoT).</summary>
    public void ApplyVitalityDamage(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Damage amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatants cannot receive damage.");

        CurrentVitality = Math.Max(0, CurrentVitality - amount);
        Touch();
    }

    public void GainGuard(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Guard amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatants cannot gain guard.");

        CurrentGuard += amount;
        Touch();
    }

    public void ResetGuardToBase(int baseGuard)
    {
        if (IsDefeated) return;

        if (CurrentGuard < baseGuard)
            CurrentGuard = baseGuard;

        Touch();
    }

    public void ApplyHeal(int maxVitality, int amount)
    {
        if (amount <= 0)
            throw new DomainException("Heal amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatant cannot be healed.");

        CurrentVitality = Math.Min(maxVitality, CurrentVitality + amount);
        Touch();
    }

    public void Revive(int maxVitality, int vitality)
    {
        if (!IsDefeated)
            throw new DomainException("Only a defeated combatant can be revived.");
        if (vitality <= 0)
            throw new DomainException("Revive vitality must be greater than zero.");

        CurrentVitality = Math.Min(maxVitality, vitality);
        CurrentGuard = 0;
        Touch();
    }

    public void GainMana(int amount)
    {
        if (amount < 0)
            throw new DomainException("Mana gain amount cannot be negative.");

        CurrentMana = Math.Min(MaxMana, CurrentMana + amount);
        Touch();
    }

    public void SpendMana(int amount)
    {
        if (amount <= 0)
            return;

        CurrentMana = Math.Max(0, CurrentMana - amount);
        Touch();
    }

    public void SpendCharge(decimal amount)
    {
        if (amount <= 0)
            return;

        CurrentCharge = Math.Max(0, CurrentCharge - amount);
        Touch();
    }

    public void GainCharge(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("Charge gain amount cannot be negative.");

        CurrentCharge = Math.Min(5m, CurrentCharge + Math.Round(amount, 1));
        Touch();
    }

    public void MarkDefeated()
    {
        CurrentVitality = 0;
        Touch();
    }

    /// <summary>Adds to accumulated threat (floored at 0). No-op for non-positive amounts.</summary>
    public void AccrueThreat(double amount)
    {
        if (amount <= 0) return;

        ThreatValue += amount;
        Touch();
    }

    /// <summary>Records who most recently damaged this combatant.</summary>
    public void RecordLastAttacker(Guid attackerId)
    {
        LastAttackerId = attackerId;
        Touch();
    }

    /// <summary>
    /// Flags a "coup très puissant" (≥25% of MaxVitality in one hit, the Bestiaire's
    /// uniform threshold) since this combatant's last action. Never clears the flag for
    /// a sub-threshold hit — one qualifying hit is enough, even if followed by smaller
    /// ones, until a script actually consumes it.
    /// </summary>
    public void RecordDamageTaken(int amount, int maxVitality)
    {
        if (maxVitality <= 0 || amount <= 0) return;

        if (amount * 4 >= maxVitality)
        {
            TookPowerfulHitSinceLastAction = true;
            Touch();
        }
    }

    /// <summary>One-shot read: returns whether a qualifying hit landed since the last
    /// action, and clears the flag so it doesn't re-trigger next turn.</summary>
    public bool ConsumePowerfulHitSinceLastAction()
    {
        if (!TookPowerfulHitSinceLastAction) return false;

        TookPowerfulHitSinceLastAction = false;
        Touch();
        return true;
    }

    public void DebugSetVitals(int maxVitality, int vitality, int guard)
    {
        if (vitality < 0 || vitality > maxVitality)
            throw new DomainException("Vitality must be between zero and max vitality.");

        if (guard < 0)
            throw new DomainException("Guard cannot be negative.");

        CurrentVitality = vitality;
        CurrentGuard = vitality == 0 ? 0 : guard;
        Touch();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static CombatantRuntimeState Rehydrate(
        Guid id,
        int currentVitality,
        int currentGuard,
        int currentFocus,
        int currentMana,
        decimal currentCharge,
        DateTime updatedAtUtc,
        double threatValue = 0,
        Guid? lastAttackerId = null,
        int maxMana = int.MaxValue,
        bool tookPowerfulHitSinceLastAction = false)
    {
        return new CombatantRuntimeState(
            id,
            currentVitality,
            currentGuard,
            currentFocus,
            currentMana,
            currentCharge,
            updatedAtUtc,
            threatValue,
            lastAttackerId,
            maxMana,
            tookPowerfulHitSinceLastAction);
    }
}
