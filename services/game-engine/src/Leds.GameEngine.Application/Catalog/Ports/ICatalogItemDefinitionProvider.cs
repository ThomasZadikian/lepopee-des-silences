using Leds.GameEngine.Application.Catalog.Contracts;

namespace Leds.GameEngine.Application.Catalog.Ports;

public interface ICatalogItemDefinitionProvider
{
    Task<CatalogItemDefinitionSnapshot?> GetItemDefinitionAsync(
        string itemDefinitionKey,
        CancellationToken cancellationToken = default);
}
