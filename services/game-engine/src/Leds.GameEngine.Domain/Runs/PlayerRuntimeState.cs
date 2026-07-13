using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public sealed class PlayerRuntimeState
{
    private readonly List<PlayerRuntimeSkill> _skills;

    private PlayerRuntimeState(
        int maxVitality,
        int currentVitality,
        int guard,
        int mana,
        int maxMana,
        int charge,
        IReadOnlyCollection<PlayerRuntimeSkill> skills)
    {
        MaxVitality = maxVitality;
        CurrentVitality = currentVitality;
        Guard = guard;
        Mana = mana;
        MaxMana = maxMana;
        Charge = charge;
        _skills = skills.ToList();
    }

    public int MaxVitality { get; }
    public int CurrentVitality { get; private set; }
    public int Guard { get; private set; }
    public int Mana { get; private set; }
    public int MaxMana { get; }
    public int Charge { get; private set; }
    public IReadOnlyCollection<PlayerRuntimeSkill> Skills => _skills.AsReadOnly();

    public bool IsDefeated => CurrentVitality <= 0;

    public static PlayerRuntimeState Create(
        int maxVitality,
        IReadOnlyCollection<PlayerRuntimeSkill> skills,
        int currentVitality = 0,
        int guard = 0,
        int mana = 0,
        int charge = 0,
        int? maxMana = null)
    {
        if (maxVitality <= 0)
            throw new DomainException("Max vitality must be greater than zero.");

        if (skills is null || skills.Count == 0)
            throw new DomainException("Player must have at least one skill.");

        var resolvedCurrentVitality = currentVitality > 0 ? currentVitality : maxVitality;

        if (resolvedCurrentVitality > maxVitality)
            throw new DomainException("Current vitality cannot exceed max vitality.");

        var resolvedMaxMana = maxMana ?? int.MaxValue;

        if (mana > resolvedMaxMana)
            throw new DomainException("Mana cannot exceed max mana.");

        return new PlayerRuntimeState(maxVitality, resolvedCurrentVitality, guard, mana, resolvedMaxMana, charge, skills);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Damage amount must be positive.");

        if (IsDefeated)
            throw new DomainException("Defeated player cannot take damage.");

        var remainingDamage = amount;

        if (Guard > 0)
        {
            var absorbed = Math.Min(Guard, remainingDamage);
            Guard -= absorbed;
            remainingDamage -= absorbed;
        }

        if (remainingDamage <= 0)
            return;

        CurrentVitality = Math.Max(0, CurrentVitality - remainingDamage);
    }

    /// <summary>
    /// Direct vitality loss that bypasses guard (e.g. NPC poison), clamped to a floor
    /// (1 out of combat so a map encounter never defeats the player).
    /// </summary>
    public void LoseVitality(int amount, int floor = 0)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentVitality = Math.Max(floor, CurrentVitality - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Heal amount must be positive.");

        if (IsDefeated)
            throw new DomainException("Defeated player cannot be healed.");

        CurrentVitality = Math.Min(MaxVitality, CurrentVitality + amount);
    }

    public void GainGuard(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Guard amount must be positive.");

        if (IsDefeated)
            throw new DomainException("Defeated player cannot gain guard.");

        Guard += amount;
    }

    public void SpendMana(int amount)
    {
        if (amount < 0)
            throw new DomainException("Mana spend amount cannot be negative.");

        if (amount > Mana)
            throw new DomainException("Insufficient mana.");

        Mana -= amount;
    }

    public void GainMana(int amount)
    {
        if (amount < 0)
            throw new DomainException("Mana gain amount cannot be negative.");

        Mana = Math.Min(MaxMana, Mana + amount);
    }

    public void SpendCharge(int amount)
    {
        if (amount < 0)
            throw new DomainException("Charge spend amount cannot be negative.");

        if (amount > Charge)
            throw new DomainException("Insufficient charge.");

        Charge -= amount;
    }

    public void GainCharge(int amount)
    {
        if (amount < 0)
            throw new DomainException("Charge gain amount cannot be negative.");

        Charge += amount;
    }

    public void SyncFromCombat(int currentVitality, int guard, int mana, int charge)
    {
        CurrentVitality = Math.Max(0, Math.Min(MaxVitality, currentVitality));
        Guard = Math.Max(0, guard);
        Mana = Math.Max(0, Math.Min(MaxMana, mana));
        Charge = Math.Max(0, charge);
    }

    /// <summary>
    /// Rehydrates player runtime state from a trusted persistence snapshot.
    /// This method must not be used to create a new gameplay player state.
    /// </summary>
    public static PlayerRuntimeState Rehydrate(
        int maxVitality,
        int currentVitality,
        int guard,
        int mana,
        int charge,
        IReadOnlyCollection<PlayerRuntimeSkill> skills,
        int maxMana = int.MaxValue)
    {
        return new PlayerRuntimeState(maxVitality, currentVitality, guard, mana, maxMana, charge, skills);
    }
}