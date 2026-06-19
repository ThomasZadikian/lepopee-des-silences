using Leds.Catalog.Application.RewardTemplates.Dtos;

namespace Leds.Catalog.Application.RewardTemplates.Ports;

public interface IRewardTemplateReadStore
{
    Task<RewardTemplateDto?> GetDtoByKeyAsync(
        string key,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RewardTemplateDto>> ListEligibleDtosAsync(
        RewardTemplateEligibilityQueryContext context,
        CancellationToken cancellationToken);
}

public sealed record RewardTemplateEligibilityQueryContext(
    string SourceType,
    int? Depth,
    string? CombatTier,
    double? DifficultyMultiplier,
    double? RewardPowerMultiplier);
