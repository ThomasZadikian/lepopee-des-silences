using Leds.GameEngine.Application.Catalog.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class ListActiveItemDefinitionsQueryHandler
    : IRequestHandler<ListActiveItemDefinitionsQuery, ListActiveItemDefinitionsResponse>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public ListActiveItemDefinitionsQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task<ListActiveItemDefinitionsResponse> Handle(
        ListActiveItemDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var definitions = await _catalogGateway.ListActiveItemDefinitionsAsync(cancellationToken);

        return new ListActiveItemDefinitionsResponse(
            definitions.Select(d => new ItemDefinitionView(
                d.Key,
                d.DisplayName,
                d.Description,
                d.Category,
                d.ItemType,
                d.Rarity,
                d.EffectRunType,
                d.EffectValue)).ToArray());
    }
}
