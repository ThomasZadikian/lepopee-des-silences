using Leds.Catalog.Domain.Enemies;

namespace Leds.Catalog.Application.Enemies.Definitions.Dtos;

public sealed record EnemyDefinitionDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string Archetype,
    int BaseDifficulty,
    int MinRiskLevel,
    int MaxRiskLevel,
    IReadOnlyCollection<string> CompatibleRoomTypes,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> SkillKeys,
    int AttackPower = 0,
    int Defense = 0,
    int Speed = 10)
{
    public static EnemyDefinitionDto FromDomain(IEnemyDefinition definition)
    {
        return new EnemyDefinitionDto(
            definition.Id.Value,
            definition.Key.Value,
            definition.Name.Value,
            definition.Description.Value,
            definition.Version.Value,
            definition.Status.ToString(),
            definition.Archetype,
            definition.BaseDifficulty,
            definition.MinRiskLevel,
            definition.MaxRiskLevel,
            definition.CompatibleRoomTypes.ToArray(),
            definition.Tags.ToArray(),
            definition.SkillKeys.ToArray(),
            definition.AttackPower,
            definition.Defense,
            definition.Speed);
    }
}
