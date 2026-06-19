namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record RewardTemplateEligibilityContext(
    string SourceType,
    int? Depth,
    string? CombatTier,
    double? DifficultyMultiplier,
    double? RewardPowerMultiplier);
