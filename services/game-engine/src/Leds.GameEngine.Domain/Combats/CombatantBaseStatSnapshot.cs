using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed class CombatantBaseStatSnapshot
{
    private CombatantBaseStatSnapshot(
        Guid id,
        int maxVitality,
        int attackPower,
        int defense,
        int startingGuard,
        int speed,
        int initiative,
        int recovery,
        int focus,
        int mana,
        int charge,
        int? atbReadyThreshold,
        DateTime createdAtUtc)
    {
        Id = id;
        MaxVitality = maxVitality;
        AttackPower = attackPower;
        Defense = defense;
        StartingGuard = startingGuard;
        Speed = speed;
        Initiative = initiative;
        Recovery = recovery;
        Focus = focus;
        Mana = mana;
        Charge = charge;
        AtbReadyThreshold = atbReadyThreshold;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public int MaxVitality { get; }
    public int AttackPower { get; }
    public int Defense { get; }
    public int StartingGuard { get; }
    public int Speed { get; }
    public int Initiative { get; }
    public int Recovery { get; }
    public int Focus { get; }
    public int Mana { get; }
    public int Charge { get; }
    public int? AtbReadyThreshold { get; }
    public DateTime CreatedAtUtc { get; }

    public static CombatantBaseStatSnapshot Create(
        int maxVitality,
        int attackPower,
        int defense,
        int startingGuard,
        int speed,
        int initiative,
        int recovery,
        int focus,
        int mana,
        int charge,
        int? atbReadyThreshold = null)
    {
        if (maxVitality <= 0)
            throw new DomainException("Max vitality must be greater than zero.");

        if (attackPower < 0)
            throw new DomainException("Attack power cannot be negative.");

        if (defense < 0)
            throw new DomainException("Defense cannot be negative.");

        if (startingGuard < 0)
            throw new DomainException("Starting guard cannot be negative.");

        if (speed <= 0)
            throw new DomainException("Speed must be greater than zero.");

        if (initiative < 0)
            throw new DomainException("Initiative cannot be negative.");

        if (recovery < 0)
            throw new DomainException("Recovery cannot be negative.");

        if (focus < 0)
            throw new DomainException("Focus cannot be negative.");

        if (mana < 0)
            throw new DomainException("Mana cannot be negative.");

        if (charge < 0)
            throw new DomainException("Charge cannot be negative.");

        return new CombatantBaseStatSnapshot(
            Guid.NewGuid(),
            maxVitality,
            attackPower,
            defense,
            startingGuard,
            speed,
            initiative,
            recovery,
            focus,
            mana,
            charge,
            atbReadyThreshold,
            DateTime.UtcNow);
    }

    public static CombatantBaseStatSnapshot Rehydrate(
        Guid id,
        int maxVitality,
        int attackPower,
        int defense,
        int startingGuard,
        int speed,
        int initiative,
        int recovery,
        int focus,
        int mana,
        int charge,
        int? atbReadyThreshold,
        DateTime createdAtUtc)
    {
        return new CombatantBaseStatSnapshot(
            id,
            maxVitality,
            attackPower,
            defense,
            startingGuard,
            speed,
            initiative,
            recovery,
            focus,
            mana,
            charge,
            atbReadyThreshold,
            createdAtUtc);
    }
}
