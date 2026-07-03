using Leds.Catalog.Application.Enemies.Loot.Dtos;
using Leds.Catalog.Application.Enemies.Loot.Ports;
using MediatR;

namespace Leds.Catalog.Application.Enemies.Loot.GetEnemyLootTableByKey;

public sealed class GetEnemyLootTableByKeyQueryHandler
    : IRequestHandler<GetEnemyLootTableByKeyQuery, GetEnemyLootTableByKeyResponse>
{
    private readonly IEnemyLootTableReadStore _readStore;

    public GetEnemyLootTableByKeyQueryHandler(IEnemyLootTableReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetEnemyLootTableByKeyResponse> Handle(
        GetEnemyLootTableByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var table = await _readStore.GetByEnemyKeyAsync(request.EnemyKey, cancellationToken);

        return new GetEnemyLootTableByKeyResponse(
            table is null ? null : EnemyLootTableDto.FromDomain(table));
    }
}
