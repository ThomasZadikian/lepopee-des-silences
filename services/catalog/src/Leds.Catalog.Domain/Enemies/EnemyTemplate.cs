using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Enemies;

public sealed class EnemyTemplate : CatalogContentBase
{
    private EnemyTemplate(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        EnemyArchetype archetype,
        CombatElement element,
        int maxHealth,
        int strength,
        int intelligence,
        int speed,
        int physicalResistance,
        int magicalResistance,
        int experienceReward,
        int goldReward)
        : base(id, key, name, description, version, status)
    {
        Archetype = archetype;
        Element = element;
        MaxHealth = maxHealth;
        Strength = strength;
        Intelligence = intelligence;
        Speed = speed;
        PhysicalResistance = physicalResistance;
        MagicalResistance = magicalResistance;
        ExperienceReward = experienceReward;
        GoldReward = goldReward;
    }

    public EnemyArchetype Archetype { get; }

    public CombatElement Element { get; }

    public int MaxHealth { get; }

    public int Strength { get; }

    public int Intelligence { get; }

    public int Speed { get; }

    public int PhysicalResistance { get; }

    public int MagicalResistance { get; }

    public int ExperienceReward { get; }

    public int GoldReward { get; }

    public static EnemyTemplate Create(
        string key,
        string name,
        string? description,
        string version,
        EnemyArchetype archetype,
        CombatElement element,
        int maxHealth,
        int strength,
        int intelligence,
        int speed,
        int physicalResistance,
        int magicalResistance,
        int experienceReward,
        int goldReward,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        if (archetype == EnemyArchetype.Unknown)
        {
            throw new DomainException("Enemy archetype is required.");
        }

        if (maxHealth <= 0)
        {
            throw new DomainException("Enemy max health must be greater than 0.");
        }

        EnsureStatIsValid(strength, "Enemy strength");
        EnsureStatIsValid(intelligence, "Enemy intelligence");
        EnsureStatIsValid(speed, "Enemy speed");
        EnsureResistanceIsValid(physicalResistance, "Enemy physical resistance");
        EnsureResistanceIsValid(magicalResistance, "Enemy magical resistance");
        EnsureRewardIsValid(experienceReward, "Enemy experience reward");
        EnsureRewardIsValid(goldReward, "Enemy gold reward");

        return new EnemyTemplate(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            CatalogContentDescription.From(description),
            CatalogContentVersion.From(version),
            status,
            archetype,
            element,
            maxHealth,
            strength,
            intelligence,
            speed,
            physicalResistance,
            magicalResistance,
            experienceReward,
            goldReward);
    }

    private static void EnsureStatIsValid(int value, string fieldName)
    {
        if (value is < 0 or > 999)
        {
            throw new DomainException($"{fieldName} must be between 0 and 999.");
        }
    }

    private static void EnsureResistanceIsValid(int value, string fieldName)
    {
        if (value is < 0 or > 100)
        {
            throw new DomainException($"{fieldName} must be between 0 and 100.");
        }
    }

    private static void EnsureRewardIsValid(int value, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }
    }
}