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
        int focus,
        int mana,
        int charge,
        int magicAttack,
        int magicDefense,
        int movement)
    {
        MaxVitality = maxVitality;
        AttackPower = attackPower;
        Defense = defense;
        StartingGuard = startingGuard;
        Speed = speed;
        Initiative = initiative;
        Focus = focus;
        Mana = mana;
        Charge = charge;
        MagicAttack = magicAttack;
        MagicDefense = magicDefense;
        Movement = movement;
    }

    public int MaxVitality { get; }
    public int AttackPower { get; }
    public int Defense { get; }
    public int StartingGuard { get; }
    public int Speed { get; }
    public int Initiative { get; }
    public int Focus { get; }
    public int Mana { get; }
    public int Charge { get; }
    public int MagicAttack { get; }
    public int MagicDefense { get; }
    public int Movement { get; }

    public static PlayerCharacterStatBlock Create(
        int maxVitality,
        int attackPower,
        int defense,
        int startingGuard,
        int speed,
        int initiative,
        int focus,
        int mana,
        int charge,
        int magicAttack = 0,
        int magicDefense = 0,
        int movement = 4)
    {
        if (maxVitality <= 0) throw new DomainException("Max vitality must be greater than zero.");
        if (attackPower < 0) throw new DomainException("Attack power cannot be negative.");
        if (defense < 0) throw new DomainException("Defense cannot be negative.");
        if (startingGuard < 0) throw new DomainException("Starting guard cannot be negative.");
        if (speed <= 0) throw new DomainException("Speed must be greater than zero.");
        if (initiative < 0) throw new DomainException("Initiative cannot be negative.");
        if (focus < 0) throw new DomainException("Focus cannot be negative.");
        if (mana < 0) throw new DomainException("Mana cannot be negative.");
        if (charge < 0) throw new DomainException("Charge cannot be negative.");
        if (magicAttack < 0) throw new DomainException("Magic attack cannot be negative.");
        if (magicDefense < 0) throw new DomainException("Magic defense cannot be negative.");
        if (movement < 1) throw new DomainException("Movement must be at least one.");

        return new PlayerCharacterStatBlock(
            maxVitality,
            attackPower,
            defense,
            startingGuard,
            speed,
            initiative,
            focus,
            mana,
            charge,
            magicAttack,
            magicDefense,
            movement);
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
            focus: 0,
            // Base Mana = 85% of base MaxVitality (design rule applied uniformly to
            // the protagonist, every recruited companion, and every enemy — see
            // CombatFactory for the enemy side, computed off scaled Vitality).
            mana: 85,
            charge: 0,
            // Same 2:1 ratio as AttackPower/Defense, halved since the starter kit
            // (skill.basic.strike/guard) is physical-only.
            magicAttack: 6,
            magicDefense: 3,
            movement: 4);
    }

}
