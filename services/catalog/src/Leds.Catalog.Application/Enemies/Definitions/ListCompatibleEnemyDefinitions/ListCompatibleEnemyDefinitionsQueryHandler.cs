using Leds.Catalog.Application.Enemies.Definitions.Dtos;
using Leds.Catalog.Application.Enemies.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Enemies.Definitions.ListCompatibleEnemyDefinitions;

public sealed class ListCompatibleEnemyDefinitionsQueryHandler
    : IRequestHandler<ListCompatibleEnemyDefinitionsQuery, ListCompatibleEnemyDefinitionsResponse>
{
    private readonly IEnemyDefinitionReadStore _readStore;

    public ListCompatibleEnemyDefinitionsQueryHandler(IEnemyDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListCompatibleEnemyDefinitionsResponse> Handle(
        ListCompatibleEnemyDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListCompatibleAsync(
            request.RoomType, request.RiskLevel, cancellationToken);

        return new ListCompatibleEnemyDefinitionsResponse(
            definitions
                .Select(EnemyDefinitionDto.FromDomain)
                .ToArray());
    }
}
