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
    string EmotionalRegister,
    int AttackPower = 0,
    int Defense = 0,
    int Speed = 10,
    int Focus = 0,
    int MagicAttack = 0,
    int MagicDefense = 0,
    int Mana = 0,
    int Movement = 4);
