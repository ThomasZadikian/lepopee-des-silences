namespace Leds.GameEngine.Application.Players;

public sealed record PlayerProfileView(
    Guid Id,
    string DisplayName,
    IReadOnlyCollection<PlayerCharacterView> Characters,
    PlayerProgressionView Progression);

public sealed record PlayerCharacterView(
    Guid Id,
    string DefinitionKey,
    string DisplayName,
    IReadOnlyCollection<PlayerCharacterSkillView> Skills,
    PlayerCharacterStatsView Stats,
    int MaxEquippedSkills);

public sealed record PlayerCharacterSkillView(
    string SkillKey,
    DateTimeOffset UnlockedAtUtc,
    string? Source,
    bool IsEquipped);

public sealed record PlayerCharacterStatsView(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Recovery,
    int Focus,
    int Mana,
    int Charge);

public sealed record PlayerProgressionView(
    int UnspentStatPoints,
    int TotalStatPointsEarned);
