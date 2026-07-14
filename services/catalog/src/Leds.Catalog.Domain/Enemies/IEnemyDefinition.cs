using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.Enemies;

public interface IEnemyDefinition : ICatalogContent
{
    string Archetype { get; }

    IReadOnlyCollection<string> CompatibleRoomTypes { get; }

    int BaseDifficulty { get; }

    int MinRiskLevel { get; }

    int MaxRiskLevel { get; }

    int AttackPower { get; }

    int Defense { get; }

    int Speed { get; }

    int Focus { get; }

    IReadOnlyCollection<string> Tags { get; }

    IReadOnlyCollection<string> SkillKeys { get; }

    int Initiative { get; }

    int Mana { get; }

    int MagicAttack { get; }

    int MagicDefense { get; }

    int Menace { get; }

    string Rarity { get; }

    string? Registre { get; }

    IReadOnlyCollection<string> BoundRoomKeys { get; }
}
