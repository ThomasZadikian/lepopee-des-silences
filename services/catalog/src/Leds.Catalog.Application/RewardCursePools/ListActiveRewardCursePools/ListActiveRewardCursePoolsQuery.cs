using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.RewardCursePools.ListActiveRewardCursePools;

public sealed record ListActiveRewardCursePoolsQuery()
    : IQuery<ListActiveRewardCursePoolsResponse>;