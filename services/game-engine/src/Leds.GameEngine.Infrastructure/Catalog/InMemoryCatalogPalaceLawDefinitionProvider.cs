using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;

namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class InMemoryCatalogPalaceLawDefinitionProvider : ICatalogPalaceLawDefinitionProvider
{
    private readonly Dictionary<string, CatalogPalaceLawDefinitionSnapshot> _laws = new()
    {
        ["law-silence-v1"] = new CatalogPalaceLawDefinitionSnapshot(
            "law-silence-v1",
            "1.0.0",
            "Loi du Silence",
            "Une Loi ancienne qui pèse sur les choix du joueur.",
            null,
            "Run",
            "UntilRunEnds",
            null,
            5,
            "Visible",
            "effectset.law-silence-v1"),
        ["law-fracture-v1"] = new CatalogPalaceLawDefinitionSnapshot(
            "law-fracture-v1",
            "1.0.0",
            "Loi de la Fracture",
            "Brise les équilibres pour mieux les reconstruire.",
            null,
            "Run",
            "UntilRunEnds",
            null,
            7,
            "Visible",
            "effectset.law-fracture-v1"),
    };

    public void Register(CatalogPalaceLawDefinitionSnapshot law)
    {
        _laws[law.Key] = law;
    }

    public Task<CatalogPalaceLawDefinitionSnapshot?> GetByKeyAsync(
        string lawDefinitionKey,
        CancellationToken cancellationToken = default)
    {
        _laws.TryGetValue(lawDefinitionKey, out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<CatalogPalaceLawDefinitionSnapshot>> ListAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<CatalogPalaceLawDefinitionSnapshot>>(
            _laws.Values.ToList().AsReadOnly());
    }
}
