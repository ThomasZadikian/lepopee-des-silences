namespace Leds.GameEngine.Application.Combats.EncounterDrafts;

public sealed record CombatEncounterDraftEnemy(
    string EnemyKey,
    string DisplayName,
    string Description,
    string Archetype,
    int BaseDifficulty,
    int MinRiskLevel,
    int MaxRiskLevel,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> SkillKeys,
    IReadOnlyCollection<CombatEncounterDraftSkill> Skills,
    int AttackPower = 0,
    int Defense = 0,
    int Speed = 10);