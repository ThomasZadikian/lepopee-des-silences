using Leds.GameEngine.Application.Combats.EncounterDrafts;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatEncounterDraftEnemyDto(
    string EnemyKey,
    string DisplayName,
    string Description,
    string Archetype,
    int BaseDifficulty,
    int MinRiskLevel,
    int MaxRiskLevel,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> SkillKeys)
{
    public static CombatEncounterDraftEnemyDto FromDomain(CombatEncounterDraftEnemy enemy)
    {
        return new CombatEncounterDraftEnemyDto(
            EnemyKey: enemy.EnemyKey,
            DisplayName: enemy.DisplayName,
            Description: enemy.Description,
            Archetype: enemy.Archetype,
            BaseDifficulty: enemy.BaseDifficulty,
            MinRiskLevel: enemy.MinRiskLevel,
            MaxRiskLevel: enemy.MaxRiskLevel,
            Tags: enemy.Tags,
            SkillKeys: enemy.SkillKeys);
    }
}
