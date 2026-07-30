namespace Leds.GameEngine.Application.Players;

public sealed record PlayerProfileView(
    Guid Id,
    string DisplayName,
    IReadOnlyCollection<PlayerCharacterView> Characters,
    PlayerProgressionView Progression,
    IReadOnlyCollection<PlayerPermanentItemView>? PermanentItems = null);

public sealed record PlayerCharacterView(
    Guid Id,
    string DefinitionKey,
    string DisplayName,
    IReadOnlyCollection<PlayerCharacterSkillView> Skills,
    PlayerCharacterStatsView Stats,
    int MaxEquippedSkills,
    IReadOnlyCollection<PlayerCharacterItemView>? Items = null,
    int MaxEquippedItems = 3,
    string CharacterType = "Standard");

public sealed record PlayerCharacterSkillView(
    string SkillKey,
    DateTimeOffset UnlockedAtUtc,
    string? Source,
    bool IsEquipped);

public sealed record PlayerCharacterItemView(
    string ItemKey,
    DateTimeOffset AcquiredAtUtc,
    string? Source,
    bool IsEquipped,
    string Slot = "Relic");

public sealed record PlayerPermanentItemView(
    string ItemDefinitionKey,
    Guid? SourceRunId,
    DateTimeOffset AcquiredAtUtc,
    string? ContainedLiquidDefinitionKey = null);

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
    int Charge,
    int MagicAttack = 0,
    int MagicDefense = 0);

public sealed record PlayerProgressionView(
    int UnspentStatPoints,
    int TotalStatPointsEarned,
    int PalaceShardCount = 0);
