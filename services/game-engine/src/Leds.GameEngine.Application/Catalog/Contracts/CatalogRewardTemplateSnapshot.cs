namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogRewardTemplateSnapshot(
    string Key,
    string Version,
    string DisplayName,
    string Description,
    string SourceType,
    int MinChoices,
    int MaxChoices,
    IReadOnlyCollection<CatalogRewardTemplateOptionSnapshot> Options);

public sealed record CatalogRewardTemplateOptionSnapshot(
    string RewardType,
    string Label,
    string Description,
    string? PayloadKey,
    string? PayloadType,
    string? EffectSetKey,
    int BaseAmount,
    string ScalingMode,
    int Weight,
    string? ItemType = null,
    string? ItemRarity = null,
    string? ItemEffectType = null);
