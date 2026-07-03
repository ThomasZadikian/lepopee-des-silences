using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.Application.Rewards.Dtos;

public sealed record RewardOfferDto(
    Guid Id,
    string Source,
    string State,
    IReadOnlyCollection<RewardChoiceDto> Choices,
    Guid? SelectedChoiceId,
    CombatScalingDto? CombatScaling,
    IReadOnlyCollection<DefeatedEnemySummaryDto> DefeatedEnemies)
{
    public static RewardOfferDto FromDomain(RewardOffer offer)
    {
        return new RewardOfferDto(
            offer.Id.Value,
            offer.Source.ToString(),
            offer.State.ToString(),
            offer.Choices.Select(RewardChoiceDto.FromDomain).ToArray(),
            offer.SelectedChoiceId?.Value,
            offer.CombatScaling is { } s ? CombatScalingDto.FromDomain(s) : null,
            offer.DefeatedEnemies.Select(DefeatedEnemySummaryDto.FromDomain).ToArray());
    }
}

public sealed record DefeatedEnemyLootEntryDto(
    string ItemKey,
    string ItemDisplayName,
    string Rarity,
    int DropPercent)
{
    public static DefeatedEnemyLootEntryDto FromDomain(DefeatedEnemyLootEntry entry) =>
        new(entry.ItemKey, entry.ItemDisplayName, entry.Rarity, entry.DropPercent);
}

public sealed record DefeatedEnemySummaryDto(
    string EnemyKey,
    string DisplayName,
    string Description,
    int Count,
    IReadOnlyCollection<DefeatedEnemyLootEntryDto> LootEntries)
{
    public static DefeatedEnemySummaryDto FromDomain(DefeatedEnemySummary summary) =>
        new(
            summary.EnemyKey,
            summary.DisplayName,
            summary.Description,
            summary.Count,
            summary.LootEntries.Select(DefeatedEnemyLootEntryDto.FromDomain).ToArray());
}

public sealed record RewardChoiceDto(
    Guid Id,
    string RewardType,
    string Label,
    string Description,
    string PayloadKey,
    string? SourceEnemyDisplayName)
{
    public static RewardChoiceDto FromDomain(RewardChoice choice)
    {
        return new RewardChoiceDto(
            choice.Id.Value,
            choice.RewardType.ToString(),
            choice.Label,
            choice.Description,
            choice.PayloadKey,
            choice.SourceEnemyDisplayName);
    }
}

/// <summary>
/// Exposes the combat risk-scaling metadata attached to a combat reward offer.
/// Useful for future item-generation pipelines and for test assertions.
/// </summary>
public sealed record CombatScalingDto(
    string CombatTier,
    int BaseRisk,
    int ActualRisk,
    int RiskDelta,
    double DifficultyMultiplier,
    double RewardPowerMultiplier,
    string RiskBand)
{
    public static CombatScalingDto FromDomain(CombatRiskProfile profile) =>
        new(
            profile.Tier.ToString(),
            profile.BaseRisk,
            profile.ActualRisk,
            profile.RiskDelta,
            profile.DifficultyMultiplier,
            profile.RewardPowerMultiplier,
            profile.RiskBand.ToString());
}