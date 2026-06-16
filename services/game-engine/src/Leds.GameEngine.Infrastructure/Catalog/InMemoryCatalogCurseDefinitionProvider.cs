using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;

namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class InMemoryCatalogCurseDefinitionProvider : ICatalogCurseDefinitionProvider
{
    private readonly Dictionary<string, CatalogCurseDefinitionSnapshot> _curses = new()
    {
        ["curse.old-wound"] = new CatalogCurseDefinitionSnapshot(
            "curse.old-wound",
            "1.0.0",
            "Vieille blessure",
            "Une blessure ancienne qui se rouvre au plus mauvais moment.",
            "Le corps porte ses propres souvenirs.",
            3,
            "NextCombatOnly",
            null,
            "effectset.curse-old-wound"),
        ["curse.weight-of-silence"] = new CatalogCurseDefinitionSnapshot(
            "curse.weight-of-silence",
            "1.0.0",
            "Poids du silence",
            "Le silence devient une charge mentale supplémentaire.",
            null,
            5,
            "NextCombatOnly",
            null,
            "effectset.curse-weight-of-silence"),
    };

    public void Register(CatalogCurseDefinitionSnapshot curse)
    {
        _curses[curse.Key] = curse;
    }

    public Task<CatalogCurseDefinitionSnapshot?> GetByKeyAsync(
        string curseDefinitionKey,
        CancellationToken cancellationToken = default)
    {
        _curses.TryGetValue(curseDefinitionKey, out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<CatalogCurseDefinitionSnapshot>> ListAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<CatalogCurseDefinitionSnapshot>>(
            _curses.Values.ToList().AsReadOnly());
    }
}
