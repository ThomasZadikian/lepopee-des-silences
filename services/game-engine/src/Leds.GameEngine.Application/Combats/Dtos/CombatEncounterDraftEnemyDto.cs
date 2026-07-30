using Leds.GameEngine.Application.Combats.EncounterDrafts;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatEncounterDraftEnemyDto(
    string EnemyKey,
    string DisplayName,
    string Description,
    string Archetype,
    int BaseDifficulty,
    string DifficultyLabel,
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
            DifficultyLabel: GetDifficultyLabel(enemy.BaseDifficulty),
            MinRiskLevel: enemy.MinRiskLevel,
            MaxRiskLevel: enemy.MaxRiskLevel,
            Tags: enemy.Tags,
            SkillKeys: enemy.SkillKeys,
            Skills: enemy.Skills
                .Select(CombatEncounterDraftSkillDto.FromDomain)
                .ToArray());
    }

    private static string GetDifficultyLabel(int baseDifficulty) => baseDifficulty switch
    {
        <= 1 => "Fragile",
        <= 2 => "Standard",
        <= 3 => "Résistant",
        <= 5 => "Dangereux",
        <= 7 => "Élite",
        _ => "Boss"
    };
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
    IReadOnlyCollection<string> Tags,
    int TacticalRange = 1,
    string TacticalAreaShape = "Single",
    bool RequiresLineOfSight = false,
    int Cooldown = 0,
    bool IsUltimate = false,
    string EmotionalRegister = "Neutral")
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
            Tags: skill.Tags,
            TacticalRange: skill.TacticalRange,
            TacticalAreaShape: skill.TacticalAreaShape,
            RequiresLineOfSight: skill.RequiresLineOfSight,
            Cooldown: skill.Cooldown,
            IsUltimate: skill.IsUltimate,
            EmotionalRegister: skill.EmotionalRegister);
    }
}
