using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;

namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class InMemoryCatalogEffectSetProvider : ICatalogEffectSetProvider
{
    private readonly Dictionary<string, CatalogEffectSetSnapshot> _effectSets = new();

    public void Register(CatalogEffectSetSnapshot effectSet)
    {
        _effectSets[effectSet.Key] = effectSet;
    }

    public Task<CatalogEffectSetSnapshot?> GetEffectSetAsync(
        string effectSetKey,
        CancellationToken cancellationToken = default)
    {
        _effectSets.TryGetValue(effectSetKey, out var result);
        return Task.FromResult(result);
    }
}
