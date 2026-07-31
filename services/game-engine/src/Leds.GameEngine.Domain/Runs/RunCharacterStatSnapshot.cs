using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public sealed class RunCharacterStatSnapshot
{
    private RunCharacterStatSnapshot(
        Guid id,
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
        Id = id;
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

    public Guid Id { get; }
    public int MaxVitality { get; private set; }
    public int AttackPower { get; private set; }
    public int Defense { get; private set; }
    public int StartingGuard { get; private set; }
    public int Speed { get; private set; }
    public int Initiative { get; private set; }
    public int Focus { get; private set; }
    public int Mana { get; private set; }
    public int Charge { get; private set; }
    public int MagicAttack { get; private set; }
    public int MagicDefense { get; private set; }
    public int Movement { get; private set; }

    public static RunCharacterStatSnapshot Create(
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
        if (maxVitality <= 0)
            throw new DomainException("Max vitality must be greater than zero.");

        if (attackPower <= 0)
            throw new DomainException("Attack power must be greater than zero.");

        if (defense < 0)
            throw new DomainException("Defense cannot be negative.");

        if (startingGuard < 0)
            throw new DomainException("Starting guard cannot be negative.");

        if (speed <= 0)
            throw new DomainException("Speed must be greater than zero.");

        if (initiative < 0)
            throw new DomainException("Initiative cannot be negative.");

        if (focus < 0)
            throw new DomainException("Focus cannot be negative.");

        if (mana < 0)
            throw new DomainException("Mana cannot be negative.");

        if (charge < 0)
            throw new DomainException("Charge cannot be negative.");

        if (magicAttack < 0)
            throw new DomainException("Magic attack cannot be negative.");

        if (magicDefense < 0)
            throw new DomainException("Magic defense cannot be negative.");

        if (movement < 1)
            throw new DomainException("Movement must be at least one.");

        return new RunCharacterStatSnapshot(
            Guid.NewGuid(),
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

    public static RunCharacterStatSnapshot CreateDefault()
    {
        return new RunCharacterStatSnapshot(
            Guid.NewGuid(),
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            focus: 0,
            mana: 0,
            charge: 0,
            magicAttack: 0,
            magicDefense: 0,
            movement: 4);
    }

    /// <summary>
    /// Stat-point mid-run resync ("Valider les choix"): overwrites every field with
    /// the freshly-computed values (see <see cref="Run.ReplaceCharacterStats"/>).
    /// Same validation as <see cref="Create"/>.
    /// </summary>
    public void ReplaceStats(
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
        int movement = 4)
    {
        if (maxVitality <= 0)
            throw new DomainException("Max vitality must be greater than zero.");

        if (attackPower <= 0)
            throw new DomainException("Attack power must be greater than zero.");

        if (defense < 0)
            throw new DomainException("Defense cannot be negative.");

        if (startingGuard < 0)
            throw new DomainException("Starting guard cannot be negative.");

        if (speed <= 0)
            throw new DomainException("Speed must be greater than zero.");

        if (initiative < 0)
            throw new DomainException("Initiative cannot be negative.");

        if (focus < 0)
            throw new DomainException("Focus cannot be negative.");

        if (mana < 0)
            throw new DomainException("Mana cannot be negative.");

        if (charge < 0)
            throw new DomainException("Charge cannot be negative.");

        if (magicAttack < 0)
            throw new DomainException("Magic attack cannot be negative.");

        if (magicDefense < 0)
            throw new DomainException("Magic defense cannot be negative.");

        if (movement < 1)
            throw new DomainException("Movement must be at least one.");

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

    public static RunCharacterStatSnapshot Rehydrate(
        Guid id,
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
        return new RunCharacterStatSnapshot(
            id,
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
}
