using Leds.Catalog.Application.Items.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Items.Definitions.ListActiveItemDefinitions;

public sealed class ListActiveItemDefinitionsQueryHandler
    : IRequestHandler<ListActiveItemDefinitionsQuery, ListActiveItemDefinitionsResponse>
{
    private readonly IItemDefinitionReadStore _readStore;

    public ListActiveItemDefinitionsQueryHandler(IItemDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveItemDefinitionsResponse> Handle(
        ListActiveItemDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListActiveDtosAsync(cancellationToken);

        return new ListActiveItemDefinitionsResponse(definitions);
    }
}
