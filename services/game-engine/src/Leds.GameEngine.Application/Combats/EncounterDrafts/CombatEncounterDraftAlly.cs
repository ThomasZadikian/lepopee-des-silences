namespace Leds.GameEngine.Application.Combats.EncounterDrafts;

public sealed record CombatEncounterDraftAlly(
    string AllyKey,
    string DisplayName,
    string Role,
    IReadOnlyCollection<string> Tags,
    bool IsProtagonist = false,
    int MaxVitality = 0,
    int AttackPower = 0,
    int Defense = 0,
    int StartingGuard = 0,
    int Speed = 10,
    int Initiative = 0,
    int Focus = 0,
    int Mana = 0,
    int Charge = 0,
    IReadOnlyCollection<CombatEncounterDraftSkill>? Skills = null,
    int MagicAttack = 0,
    int MagicDefense = 0,
    int Movement = 4,
    string EmotionalRegister = "neutral");
