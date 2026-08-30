using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class GetItemTypeCatalogQueryHandler
    : IRequestHandler<GetItemTypeCatalogQuery, CatalogItemTypeCatalog>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public GetItemTypeCatalogQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public Task<CatalogItemTypeCatalog> Handle(
        GetItemTypeCatalogQuery request,
        CancellationToken cancellationToken) =>
        _catalogGateway.GetItemTypeCatalogAsync(cancellationToken);
}
