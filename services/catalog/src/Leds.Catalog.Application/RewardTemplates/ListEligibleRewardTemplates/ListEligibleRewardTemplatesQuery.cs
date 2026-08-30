using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.RewardTemplates.ListEligibleRewardTemplates;

public sealed record ListEligibleRewardTemplatesQuery(
    string SourceType,
    int? Depth,
    string? CombatTier,
    double? DifficultyMultiplier,
    double? RewardPowerMultiplier)
    : IQuery<ListEligibleRewardTemplatesResponse>;
