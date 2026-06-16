using Leds.GameEngine.Application.Catalog.Contracts;

namespace Leds.GameEngine.Application.Catalog.Ports;

public interface ICatalogPalaceLawDefinitionProvider
{
    Task<CatalogPalaceLawDefinitionSnapshot?> GetByKeyAsync(
        string lawDefinitionKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogPalaceLawDefinitionSnapshot>> ListAvailableAsync(
        CancellationToken cancellationToken = default);
}
