using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.RewardTemplates.GetRewardTemplateByKey;

public sealed record GetRewardTemplateByKeyQuery(string Key)
    : IQuery<GetRewardTemplateByKeyResponse>;
