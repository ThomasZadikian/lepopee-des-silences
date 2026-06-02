using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.Combat;

namespace Leds.Catalog.Domain.Enemies;

public interface IEnemyTemplate : ICatalogContent
{
    EnemyArchetype Archetype { get; }

    CombatElement Element { get; }

    int MaxHealth { get; }

    int Strength { get; }

    int Intelligence { get; }

    int Speed { get; }

    int PhysicalResistance { get; }

    int MagicalResistance { get; }

    int ExperienceReward { get; }

    int GoldReward { get; }
}