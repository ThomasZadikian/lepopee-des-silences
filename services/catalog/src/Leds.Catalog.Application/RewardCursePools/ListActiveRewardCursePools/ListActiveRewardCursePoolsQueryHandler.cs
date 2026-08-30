using Leds.Catalog.Application.RewardCursePools.Dtos;
using Leds.Catalog.Application.RewardCursePools.Ports;
using MediatR;

namespace Leds.Catalog.Application.RewardCursePools.ListActiveRewardCursePools;

public sealed class ListActiveRewardCursePoolsQueryHandler
    : IRequestHandler<ListActiveRewardCursePoolsQuery, ListActiveRewardCursePoolsResponse>
{
    private readonly IRewardCursePoolReadStore _readStore;

    public ListActiveRewardCursePoolsQueryHandler(IRewardCursePoolReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveRewardCursePoolsResponse> Handle(
        ListActiveRewardCursePoolsQuery request,
        CancellationToken cancellationToken)
    {
        var pools = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActiveRewardCursePoolsResponse(
            pools.Select(RewardCursePoolDto.FromDomain).ToArray());
    }
}