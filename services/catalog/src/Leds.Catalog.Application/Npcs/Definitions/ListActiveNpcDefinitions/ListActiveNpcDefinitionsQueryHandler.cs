using Leds.Catalog.Application.Npcs.Definitions.Dtos;
using Leds.Catalog.Application.Npcs.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Npcs.Definitions.ListActiveNpcDefinitions;

public sealed class ListActiveNpcDefinitionsQueryHandler
    : IRequestHandler<ListActiveNpcDefinitionsQuery, ListActiveNpcDefinitionsResponse>
{
    private readonly INpcDefinitionReadStore _readStore;

    public ListActiveNpcDefinitionsQueryHandler(INpcDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveNpcDefinitionsResponse> Handle(
        ListActiveNpcDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActiveNpcDefinitionsResponse(
            definitions
                .Select(NpcDefinitionDto.FromDomain)
                .ToArray());
    }
}
