using Leds.GameEngine.Application.Catalog.Contracts;

namespace Leds.GameEngine.Application.Catalog.Ports;

public interface ICatalogEffectSetProvider
{
    Task<CatalogEffectSetSnapshot?> GetEffectSetAsync(
        string effectSetKey,
        CancellationToken cancellationToken = default);
}
