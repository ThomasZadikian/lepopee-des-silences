namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RewardOfferEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? CombatTier { get; set; }
    public decimal? DifficultyMultiplier { get; set; }
    public decimal? RewardPowerMultiplier { get; set; }
    public string? CatalogRewardTemplateKey { get; set; }
    public string? CatalogRewardTemplateVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SelectedAtUtc { get; set; }

    public RunEntity? Run { get; set; }
    public List<RewardOptionEntity> Options { get; set; } = [];
}

public sealed class RewardOptionEntity
{
    public Guid Id { get; set; }
    public Guid RewardOfferId { get; set; }
    public string RewardType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PayloadKey { get; set; }
    public string? PayloadType { get; set; }
    public string? SourceEnemyKey { get; set; }
    public string? SourceEnemyDisplayName { get; set; }
    public string? EffectSetKey { get; set; }
    public string? EffectSetVersion { get; set; }
    public int? BaseAmount { get; set; }
    public int? ScaledAmount { get; set; }
    public bool IsSelected { get; set; }
    public int SelectionOrder { get; set; }

    public RewardOfferEntity? RewardOffer { get; set; }
}
