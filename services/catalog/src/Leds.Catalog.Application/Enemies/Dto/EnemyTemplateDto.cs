using Leds.Catalog.Domain.Enemies;

namespace Leds.Catalog.Application.Enemies.Dtos;

public sealed record EnemyTemplateDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string Archetype,
    string Element,
    int MaxHealth,
    int Strength,
    int Intelligence,
    int Speed,
    int PhysicalResistance,
    int MagicalResistance,
    int ExperienceReward,
    int GoldReward)
{
    public static EnemyTemplateDto FromDomain(IEnemyTemplate enemy)
    {
        return new EnemyTemplateDto(
            enemy.Id.Value,
            enemy.Key.Value,
            enemy.Name.Value,
            enemy.Description.Value,
            enemy.Version.Value,
            enemy.Status.ToString(),
            enemy.Archetype.ToString(),
            enemy.Element.ToString(),
            enemy.MaxHealth,
            enemy.Strength,
            enemy.Intelligence,
            enemy.Speed,
            enemy.PhysicalResistance,
            enemy.MagicalResistance,
            enemy.ExperienceReward,
            enemy.GoldReward);
    }
}