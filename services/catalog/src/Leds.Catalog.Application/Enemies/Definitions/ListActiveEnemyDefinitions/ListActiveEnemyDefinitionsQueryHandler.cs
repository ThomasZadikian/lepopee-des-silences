using Leds.Catalog.Application.Enemies.Definitions.Dtos;
using Leds.Catalog.Application.Enemies.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Enemies.Definitions.ListActiveEnemyDefinitions;

public sealed class ListActiveEnemyDefinitionsQueryHandler
    : IRequestHandler<ListActiveEnemyDefinitionsQuery, ListActiveEnemyDefinitionsResponse>
{
    private readonly IEnemyDefinitionReadStore _readStore;

    public ListActiveEnemyDefinitionsQueryHandler(IEnemyDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveEnemyDefinitionsResponse> Handle(
        ListActiveEnemyDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActiveEnemyDefinitionsResponse(
            definitions
                .Select(EnemyDefinitionDto.FromDomain)
                .ToArray());
    }
}
