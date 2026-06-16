using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;

namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class InMemoryCatalogEffectSetProvider : ICatalogEffectSetProvider
{
    private readonly Dictionary<string, CatalogEffectSetSnapshot> _effectSets = new()
    {
        ["effectset.law-silence-v1"] = new CatalogEffectSetSnapshot(
            "effectset.law-silence-v1",
            "1.0.0",
            [
                new CatalogEffectDefinitionSnapshot("ModifyDifficultyMultiplier", "Run", 0.10m, "Flat", "UntilRunEnds", "Additive", null, 1, null, null, null),
                new CatalogEffectDefinitionSnapshot("ModifyRewardPowerMultiplier", "Run", 0.15m, "Flat", "UntilRunEnds", "Additive", null, 2, null, null, null),
            ]),
        ["effectset.law-fracture-v1"] = new CatalogEffectSetSnapshot(
            "effectset.law-fracture-v1",
            "1.0.0",
            [
                new CatalogEffectDefinitionSnapshot("AddStartingGuard", "Run", 15m, "Flat", "UntilRunEnds", "Additive", null, 1, null, null, null),
            ]),
        ["effectset.curse-old-wound"] = new CatalogEffectSetSnapshot(
            "effectset.curse-old-wound",
            "1.0.0",
            [
                new CatalogEffectDefinitionSnapshot("ModifyDifficultyMultiplier", "Run", 0.10m, "Flat", "NextCombatOnly", "Additive", null, 1, null, null, null),
            ]),
        ["effectset.curse-weight-of-silence"] = new CatalogEffectSetSnapshot(
            "effectset.curse-weight-of-silence",
            "1.0.0",
            [
                new CatalogEffectDefinitionSnapshot("ModifyDifficultyMultiplier", "Run", 0.20m, "Flat", "NextCombatOnly", "Additive", null, 1, null, null, null),
            ]),
    };

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
