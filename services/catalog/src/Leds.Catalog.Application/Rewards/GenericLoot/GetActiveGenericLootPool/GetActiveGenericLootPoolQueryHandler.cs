using Leds.Catalog.Application.Rewards.GenericLoot.Dtos;
using Leds.Catalog.Application.Rewards.GenericLoot.Ports;
using MediatR;

namespace Leds.Catalog.Application.Rewards.GenericLoot.GetActiveGenericLootPool;

public sealed class GetActiveGenericLootPoolQueryHandler
    : IRequestHandler<GetActiveGenericLootPoolQuery, GetActiveGenericLootPoolResponse>
{
    private readonly IGenericLootPoolReadStore _readStore;

    public GetActiveGenericLootPoolQueryHandler(IGenericLootPoolReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetActiveGenericLootPoolResponse> Handle(
        GetActiveGenericLootPoolQuery request,
        CancellationToken cancellationToken)
    {
        var pool = await _readStore.GetActiveAsync(cancellationToken);

        return new GetActiveGenericLootPoolResponse(
            pool is null ? null : GenericLootPoolDto.FromDomain(pool));
    }
}
