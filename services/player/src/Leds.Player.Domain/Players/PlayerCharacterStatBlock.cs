using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerCharacterStatBlock
{
    private PlayerCharacterStatBlock(
        int maxVitality,
        int attackPower,
        int defense,
        int startingGuard,
        int speed,
        int initiative,
        int recovery,
        int focus,
        int mana,
        int charge)
    {
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
    }

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

    public static PlayerCharacterStatBlock Create(
        int maxVitality,
        int attackPower,
        int defense,
        int startingGuard,
        int speed,
        int initiative,
        int recovery,
        int focus,
        int mana,
        int charge)
    {
        if (maxVitality <= 0) throw new DomainException("Max vitality must be greater than zero.");
        if (attackPower < 0) throw new DomainException("Attack power cannot be negative.");
        if (defense < 0) throw new DomainException("Defense cannot be negative.");
        if (startingGuard < 0) throw new DomainException("Starting guard cannot be negative.");
        if (speed <= 0) throw new DomainException("Speed must be greater than zero.");
        if (initiative < 0) throw new DomainException("Initiative cannot be negative.");
        if (recovery < 0) throw new DomainException("Recovery cannot be negative.");
        if (focus < 0) throw new DomainException("Focus cannot be negative.");
        if (mana < 0) throw new DomainException("Mana cannot be negative.");
        if (charge < 0) throw new DomainException("Charge cannot be negative.");

        return new PlayerCharacterStatBlock(
            maxVitality,
            attackPower,
            defense,
            startingGuard,
            speed,
            initiative,
            recovery,
            focus,
            mana,
            charge);
    }

    public static PlayerCharacterStatBlock CreateDefaultPorteur()
    {
        return Create(
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            recovery: 5,
            focus: 0,
            mana: 0,
            charge: 0);
    }
}
