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
        int focus,
        int mana,
        int charge,
        DateTime createdAtUtc,
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
        CreatedAtUtc = createdAtUtc;
        MagicAttack = magicAttack;
        MagicDefense = magicDefense;
        Movement = movement;
    }

    public Guid Id { get; }
    public int MaxVitality { get; }
    public int AttackPower { get; }
    public int Defense { get; }
    public int StartingGuard { get; }
    public int Speed { get; }
    public int Initiative { get; }
    public int Focus { get; }
    public int Mana { get; }
    public int Charge { get; }
    public DateTime CreatedAtUtc { get; }
    // Authored base stats mirroring AttackPower/Defense — default 0, which keeps
    // the Magic-category damage ratio neutral (see CombatSkillEffectResolver) for
    // every combatant that doesn't explicitly author these (all content predating
    // the Bestiaire chantier).
    public int MagicAttack { get; }
    public int MagicDefense { get; }
    public int Movement { get; }

    public static CombatantBaseStatSnapshot Create(
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

        return new CombatantBaseStatSnapshot(
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
            DateTime.UtcNow,
            magicAttack,
            magicDefense,
            movement);
    }

    public static CombatantBaseStatSnapshot Rehydrate(
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
        DateTime createdAtUtc,
        int magicAttack = 0,
        int magicDefense = 0,
        int movement = 4)
    {
        return new CombatantBaseStatSnapshot(
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
            createdAtUtc,
            magicAttack,
            magicDefense,
            movement);
    }
}
