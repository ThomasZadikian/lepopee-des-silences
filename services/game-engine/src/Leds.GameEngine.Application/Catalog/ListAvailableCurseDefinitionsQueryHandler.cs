using Leds.GameEngine.Application.Catalog.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class ListAvailableCurseDefinitionsQueryHandler
    : IRequestHandler<ListAvailableCurseDefinitionsQuery, ListAvailableCurseDefinitionsResponse>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public ListAvailableCurseDefinitionsQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task<ListAvailableCurseDefinitionsResponse> Handle(
        ListAvailableCurseDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var definitions = await _catalogGateway.ListAvailableCurseDefinitionsAsync(cancellationToken);

        return new ListAvailableCurseDefinitionsResponse(
            definitions.Select(d => new CurseDefinitionView(
                d.Key,
                d.DisplayName,
                d.Description,
                d.NarrativeText,
                d.Severity,
                d.Duration,
                d.Trigger)).ToArray());
    }
}
