using Leds.Catalog.Application.RewardTemplates.Dtos;

namespace Leds.Catalog.Application.RewardTemplates.ListEligibleRewardTemplates;

public sealed record ListEligibleRewardTemplatesResponse(
    IReadOnlyCollection<RewardTemplateDto> Definitions);
