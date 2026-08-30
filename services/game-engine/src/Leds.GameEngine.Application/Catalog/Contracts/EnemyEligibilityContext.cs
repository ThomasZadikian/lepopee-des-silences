namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record EnemyEligibilityContext(
    string RoomType,
    string? RoomDefinitionKey,
    int RoomDepth,
    string NodeEventType,
    int RiskLevel,
    string? RewardProfile,
    IReadOnlyCollection<string> ActiveLawKeys,
    IReadOnlyCollection<string> ActiveCurseKeys,
    IReadOnlyCollection<string> RequiredTags,
    IReadOnlyCollection<string> ExcludedTags);
