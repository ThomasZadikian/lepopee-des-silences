using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class GetEmotionalRegisterCatalogQueryHandler
    : IRequestHandler<GetEmotionalRegisterCatalogQuery, CatalogEmotionalRegisterCatalog>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public GetEmotionalRegisterCatalogQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public Task<CatalogEmotionalRegisterCatalog> Handle(
        GetEmotionalRegisterCatalogQuery request,
        CancellationToken cancellationToken) =>
        _catalogGateway.GetEmotionalRegisterCatalogAsync(cancellationToken);
}
