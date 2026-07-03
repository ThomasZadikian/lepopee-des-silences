using Leds.Catalog.Application.Worlds.Ports;
using MediatR;

namespace Leds.Catalog.Application.Worlds.ListActiveWorldDefinitions;

public sealed class ListActiveWorldDefinitionsQueryHandler
    : IRequestHandler<ListActiveWorldDefinitionsQuery, ListActiveWorldDefinitionsResponse>
{
    private readonly IWorldDefinitionReadStore _readStore;
    public ListActiveWorldDefinitionsQueryHandler(IWorldDefinitionReadStore readStore) => _readStore = readStore;

    public async Task<ListActiveWorldDefinitionsResponse> Handle(
        ListActiveWorldDefinitionsQuery request, CancellationToken cancellationToken)
        => new(await _readStore.ListActiveAsync(cancellationToken));
}
