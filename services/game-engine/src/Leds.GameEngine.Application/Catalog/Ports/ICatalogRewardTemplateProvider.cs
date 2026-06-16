using Leds.GameEngine.Application.Catalog.Contracts;

namespace Leds.GameEngine.Application.Catalog.Ports;

public interface ICatalogRewardTemplateProvider
{
    Task<CatalogRewardTemplateSnapshot?> GetRewardTemplateAsync(
        string templateKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogRewardTemplateSnapshot>> ListEligibleRewardTemplatesAsync(
        RewardTemplateEligibilityContext context,
        CancellationToken cancellationToken = default);
}

public sealed record RewardTemplateEligibilityContext(
    string SourceType,
    int? Depth,
    string? CombatTier,
    double? DifficultyMultiplier,
    double? RewardPowerMultiplier);
