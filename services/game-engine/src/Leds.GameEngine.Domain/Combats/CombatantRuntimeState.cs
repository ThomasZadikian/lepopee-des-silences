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
        int currentCharge,
        int? atbGaugeValue,
        int? actionRecoveryUntilTick,
        DateTime updatedAtUtc)
    {
        Id = id;
        CurrentVitality = currentVitality;
        CurrentGuard = currentGuard;
        CurrentFocus = currentFocus;
        CurrentMana = currentMana;
        CurrentCharge = currentCharge;
        AtbGaugeValue = atbGaugeValue;
        ActionRecoveryUntilTick = actionRecoveryUntilTick;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }
    public int CurrentVitality { get; private set; }
    public int CurrentGuard { get; private set; }
    public int CurrentFocus { get; private set; }
    public int CurrentMana { get; private set; }
    public int CurrentCharge { get; private set; }
    public int? AtbGaugeValue { get; private set; }
    public int? ActionRecoveryUntilTick { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public bool IsDefeated => CurrentVitality <= 0;

    public static CombatantRuntimeState Create(
        int currentVitality,
        int currentGuard,
        int currentFocus = 0,
        int currentMana = 0,
        int currentCharge = 0,
        int? atbGaugeValue = null,
        int? actionRecoveryUntilTick = null)
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

        return new CombatantRuntimeState(
            Guid.NewGuid(),
            currentVitality,
            currentGuard,
            currentFocus,
            currentMana,
            currentCharge,
            atbGaugeValue,
            actionRecoveryUntilTick,
            DateTime.UtcNow);
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

    public void GainMana(int amount)
    {
        if (amount < 0)
            throw new DomainException("Mana gain amount cannot be negative.");

        CurrentMana += amount;
        Touch();
    }

    public void GainCharge(int amount)
    {
        if (amount < 0)
            throw new DomainException("Charge gain amount cannot be negative.");

        CurrentCharge += amount;
        Touch();
    }

    public void MarkDefeated()
    {
        CurrentVitality = 0;
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
        int currentCharge,
        int? atbGaugeValue,
        int? actionRecoveryUntilTick,
        DateTime updatedAtUtc)
    {
        return new CombatantRuntimeState(
            id,
            currentVitality,
            currentGuard,
            currentFocus,
            currentMana,
            currentCharge,
            atbGaugeValue,
            actionRecoveryUntilTick,
            updatedAtUtc);
    }
}
