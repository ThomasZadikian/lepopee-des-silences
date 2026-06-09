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
    IReadOnlyCollection<string> SkillKeys,
    IReadOnlyCollection<CombatEncounterDraftSkillDto> Skills)
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
            SkillKeys: enemy.SkillKeys,
            Skills: enemy.Skills
                .Select(CombatEncounterDraftSkillDto.FromDomain)
                .ToArray());
    }
}

public sealed record CombatEncounterDraftSkillDto(
    string Key,
    string DisplayName,
    string Description,
    string SkillType,
    string TargetingType,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower,
    IReadOnlyCollection<string> Tags)
{
    public static CombatEncounterDraftSkillDto FromDomain(CombatEncounterDraftSkill skill)
    {
        return new CombatEncounterDraftSkillDto(
            Key: skill.Key,
            DisplayName: skill.DisplayName,
            Description: skill.Description,
            SkillType: skill.SkillType,
            TargetingType: skill.TargetingType,
            EffectType: skill.EffectType,
            ManaCost: skill.ManaCost,
            ChargeCost: skill.ChargeCost,
            BasePower: skill.BasePower,
            Tags: skill.Tags);
    }
}
