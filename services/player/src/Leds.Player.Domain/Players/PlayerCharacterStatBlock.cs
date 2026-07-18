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
        int charge,
        int magicAttack,
        int magicDefense)
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
        MagicAttack = magicAttack;
        MagicDefense = magicDefense;
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
    public int MagicAttack { get; }
    public int MagicDefense { get; }

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
        int charge,
        int magicAttack = 0,
        int magicDefense = 0)
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
        if (magicAttack < 0) throw new DomainException("Magic attack cannot be negative.");
        if (magicDefense < 0) throw new DomainException("Magic defense cannot be negative.");

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
            charge,
            magicAttack,
            magicDefense);
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
            charge: 0,
            // Same 2:1 ratio as AttackPower/Defense, halved since the starter kit
            // (skill.basic.strike/guard) is physical-only — a magic baseline of 0
            // made Magic Attack/Defense inert until stat points were spent on them.
            magicAttack: 6,
            magicDefense: 3);
    }

    // Per-point increment for each allocatable stat. Most stats grant +1; Vitality
    // and Mana grant a larger jump since one raw point of PV/PP barely moves the
    // needle otherwise.
    public const int MaxVitalityIncrementPerPoint = 10;
    public const int ManaIncrementPerPoint = 5;
    private const int DefaultIncrementPerPoint = 1;

    public PlayerCharacterStatBlock WithIncrementedStat(PlayerStatKind kind)
    {
        return kind switch
        {
            PlayerStatKind.MaxVitality => Create(MaxVitality + MaxVitalityIncrementPerPoint, AttackPower, Defense, StartingGuard, Speed, Initiative, Recovery, Focus, Mana, Charge, MagicAttack, MagicDefense),
            PlayerStatKind.AttackPower => Create(MaxVitality, AttackPower + DefaultIncrementPerPoint, Defense, StartingGuard, Speed, Initiative, Recovery, Focus, Mana, Charge, MagicAttack, MagicDefense),
            PlayerStatKind.Defense => Create(MaxVitality, AttackPower, Defense + DefaultIncrementPerPoint, StartingGuard, Speed, Initiative, Recovery, Focus, Mana, Charge, MagicAttack, MagicDefense),
            PlayerStatKind.StartingGuard => Create(MaxVitality, AttackPower, Defense, StartingGuard + DefaultIncrementPerPoint, Speed, Initiative, Recovery, Focus, Mana, Charge, MagicAttack, MagicDefense),
            PlayerStatKind.Speed => Create(MaxVitality, AttackPower, Defense, StartingGuard, Speed + DefaultIncrementPerPoint, Initiative, Recovery, Focus, Mana, Charge, MagicAttack, MagicDefense),
            PlayerStatKind.Initiative => Create(MaxVitality, AttackPower, Defense, StartingGuard, Speed, Initiative + DefaultIncrementPerPoint, Recovery, Focus, Mana, Charge, MagicAttack, MagicDefense),
            PlayerStatKind.Recovery => Create(MaxVitality, AttackPower, Defense, StartingGuard, Speed, Initiative, Recovery + DefaultIncrementPerPoint, Focus, Mana, Charge, MagicAttack, MagicDefense),
            PlayerStatKind.Focus => Create(MaxVitality, AttackPower, Defense, StartingGuard, Speed, Initiative, Recovery, Focus + DefaultIncrementPerPoint, Mana, Charge, MagicAttack, MagicDefense),
            PlayerStatKind.Mana => Create(MaxVitality, AttackPower, Defense, StartingGuard, Speed, Initiative, Recovery, Focus, Mana + ManaIncrementPerPoint, Charge, MagicAttack, MagicDefense),
            PlayerStatKind.Charge => Create(MaxVitality, AttackPower, Defense, StartingGuard, Speed, Initiative, Recovery, Focus, Mana, Charge + DefaultIncrementPerPoint, MagicAttack, MagicDefense),
            PlayerStatKind.MagicAttack => Create(MaxVitality, AttackPower, Defense, StartingGuard, Speed, Initiative, Recovery, Focus, Mana, Charge, MagicAttack + DefaultIncrementPerPoint, MagicDefense),
            PlayerStatKind.MagicDefense => Create(MaxVitality, AttackPower, Defense, StartingGuard, Speed, Initiative, Recovery, Focus, Mana, Charge, MagicAttack, MagicDefense + DefaultIncrementPerPoint),
            _ => throw new DomainException($"Unknown stat kind '{kind}'.")
        };
    }
}
