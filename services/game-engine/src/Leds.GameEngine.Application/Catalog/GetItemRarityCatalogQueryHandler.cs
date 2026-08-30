using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class GetItemRarityCatalogQueryHandler
    : IRequestHandler<GetItemRarityCatalogQuery, CatalogItemRarityCatalog>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public GetItemRarityCatalogQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public Task<CatalogItemRarityCatalog> Handle(
        GetItemRarityCatalogQuery request,
        CancellationToken cancellationToken) =>
        _catalogGateway.GetItemRarityCatalogAsync(cancellationToken);
}
