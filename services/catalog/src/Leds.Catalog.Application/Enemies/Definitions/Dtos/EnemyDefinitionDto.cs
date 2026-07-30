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
    int Speed = 10,
    int Focus = 0,
    int Initiative = 0,
    int Mana = 0,
    int MagicAttack = 0,
    int MagicDefense = 0,
    int Menace = 0,
    string Rarity = "Common",
    string? Registre = null,
    IReadOnlyCollection<string>? BoundRoomKeys = null,
    int Movement = 4)
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
            definition.Speed,
            definition.Focus,
            definition.Initiative,
            definition.Mana,
            definition.MagicAttack,
            definition.MagicDefense,
            definition.Menace,
            definition.Rarity,
            definition.Registre,
            definition.BoundRoomKeys.ToArray(),
            definition.Movement);
    }
}
