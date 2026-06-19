using Leds.Catalog.Application.RewardTemplates.Dtos;

namespace Leds.Catalog.Application.RewardTemplates.GetRewardTemplateByKey;

public sealed record GetRewardTemplateByKeyResponse(
    RewardTemplateDto? Definition);
