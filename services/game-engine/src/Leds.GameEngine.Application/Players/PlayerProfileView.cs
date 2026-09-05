namespace Leds.GameEngine.Application.Players;

public sealed record PlayerProfileView(
    Guid Id,
    string DisplayName,
    IReadOnlyCollection<PlayerCharacterView> Characters,
    PlayerProgressionView Progression,
    IReadOnlyCollection<PlayerPermanentItemView>? PermanentItems = null,
    MainStoryProgressView? MainStory = null);

public sealed record MainStoryProgressView(
    string? SequenceKey,
    string? SequenceVersion,
    string? StepKey,
    string? CheckpointKey,
    bool IsCompleted,
    int HighestDifficultyLevelUnlocked,
    IReadOnlyCollection<string> UnlockedRoomKeys,
    IReadOnlyCollection<string> VisibleRoomKeys)
{
    public static MainStoryProgressView Incomplete { get; } =
        new(null, null, null, null, false, 0, [], []);
}

public sealed record PlayerCharacterView(
    Guid Id,
    string DefinitionKey,
    string DisplayName,
    IReadOnlyCollection<PlayerCharacterSkillView> Skills,
    PlayerCharacterStatsView Stats,
    int MaxEquippedSkills,
    IReadOnlyCollection<PlayerCharacterItemView>? Items = null,
    int MaxEquippedItems = 3,
    string CharacterType = "Standard",
    string? ArchetypeKey = null,
    PlayerCharacterStatsView? BaseStats = null);

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
    string Slot = "Relic",
    Guid ItemInstanceId = default,
    string? Position = null);

public sealed record PlayerPermanentItemView(
    string ItemDefinitionKey,
    Guid? SourceRunId,
    DateTimeOffset AcquiredAtUtc,
    string? ContainedLiquidDefinitionKey = null,
    Guid ItemInstanceId = default);

public sealed record PlayerCharacterStatsView(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Focus,
    int Mana,
    int Charge,
    int MagicAttack = 0,
    int MagicDefense = 0,
    int Movement = 4);

public sealed record PlayerProgressionView(
    int PalaceShardCount = 0,
    int HimLitShardCount = 0);

public sealed record EquipmentResourceContextView(int? CurrentVitality = null, int? CurrentMana = null);

public sealed record EquipmentItemPlanView(Guid ItemInstanceId, string DefinitionKey, string DisplayName);

public sealed record EquipmentStatsView(
    int MaxVitality, int AttackPower, int MagicAttack, int Defense, int MagicDefense,
    int StartingGuard, int Speed, int Initiative, int Focus, int Mana, int Charge, int Movement);

public sealed record EquipmentStatDeltaView(string Stat, int Current, int Projected, int Delta);

public sealed record EquipmentChangePlanView(
    string TargetPosition,
    EquipmentItemPlanView CandidateItem,
    EquipmentItemPlanView? CurrentlyEquippedItem,
    bool CanEquip,
    IReadOnlyCollection<string> BlockingReasons,
    EquipmentStatsView CurrentEffectiveStats,
    EquipmentStatsView ProjectedEffectiveStats,
    IReadOnlyCollection<EquipmentStatDeltaView> StatDeltas,
    IReadOnlyCollection<string> CurrentTemporarySkills,
    IReadOnlyCollection<string> ProjectedTemporarySkills,
    IReadOnlyCollection<string> GainedTemporarySkills,
    IReadOnlyCollection<string> LostTemporarySkills,
    int CurrentVitality,
    int ProjectedCurrentVitality,
    int CurrentMana,
    int ProjectedCurrentMana,
    IReadOnlyCollection<string> AllowedSlots,
    IReadOnlyCollection<string> ProficiencyTags);
