using Leds.GameEngine.Application.Catalog.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class ListActivePalaceLawDefinitionsQueryHandler
    : IRequestHandler<ListActivePalaceLawDefinitionsQuery, ListActivePalaceLawDefinitionsResponse>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public ListActivePalaceLawDefinitionsQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task<ListActivePalaceLawDefinitionsResponse> Handle(
        ListActivePalaceLawDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var definitions = await _catalogGateway.ListActivePalaceLawDefinitionsAsync(cancellationToken);

        return new ListActivePalaceLawDefinitionsResponse(
            definitions.Select(d => new PalaceLawDefinitionView(
                d.Key,
                d.Name,
                d.Description,
                d.Rarity,
                d.Polarity,
                d.IsMajeure,
                d.ImpactDomains)).ToArray());
    }
}
