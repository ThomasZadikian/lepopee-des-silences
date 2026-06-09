using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.Enemies;

public interface IEnemyDefinition : ICatalogContent
{
    string Archetype { get; }

    IReadOnlyCollection<string> CompatibleRoomTypes { get; }

    int BaseDifficulty { get; }

    int MinRiskLevel { get; }

    int MaxRiskLevel { get; }

    IReadOnlyCollection<string> Tags { get; }

    IReadOnlyCollection<string> SkillKeys { get; }
}
