using Leds.GameEngine.Application.Catalog.Contracts;

namespace Leds.GameEngine.Application.Catalog.Ports;

public interface ICatalogCurseDefinitionProvider
{
    Task<CatalogCurseDefinitionSnapshot?> GetByKeyAsync(
        string curseDefinitionKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogCurseDefinitionSnapshot>> ListAvailableAsync(
        CancellationToken cancellationToken = default);
}
