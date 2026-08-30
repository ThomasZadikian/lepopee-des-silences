namespace Leds.Catalog.Application.RewardTemplates.Dtos;

public sealed record RewardTemplateDto(
    Guid Id,
    string Key,
    string Version,
    string DisplayName,
    string Description,
    string SourceType,
    int MinChoices,
    int MaxChoices,
    string Status,
    IReadOnlyCollection<RewardTemplateOptionDto> Options);

public sealed record RewardTemplateOptionDto(
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
