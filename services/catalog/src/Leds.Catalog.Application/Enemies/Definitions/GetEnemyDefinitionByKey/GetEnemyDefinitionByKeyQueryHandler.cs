using Leds.Catalog.Application.Enemies.Definitions.Dtos;
using Leds.Catalog.Application.Enemies.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Enemies.Definitions.GetEnemyDefinitionByKey;

public sealed class GetEnemyDefinitionByKeyQueryHandler
    : IRequestHandler<GetEnemyDefinitionByKeyQuery, GetEnemyDefinitionByKeyResponse>
{
    private readonly IEnemyDefinitionReadStore _readStore;

    public GetEnemyDefinitionByKeyQueryHandler(IEnemyDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetEnemyDefinitionByKeyResponse> Handle(
        GetEnemyDefinitionByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var definition = await _readStore.GetByKeyAsync(request.Key, cancellationToken);

        return new GetEnemyDefinitionByKeyResponse(
            definition is null ? null : EnemyDefinitionDto.FromDomain(definition));
    }
}
