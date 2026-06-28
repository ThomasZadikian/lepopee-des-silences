using Leds.Catalog.Application.RewardCursePools.Dtos;

namespace Leds.Catalog.Application.RewardCursePools.ListActiveRewardCursePools;

public sealed record ListActiveRewardCursePoolsResponse(
    IReadOnlyCollection<RewardCursePoolDto> Pools);