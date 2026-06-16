using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;

namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class InMemoryCatalogEnemyDefinitionProvider : ICatalogEnemyDefinitionProvider
{
    private readonly Dictionary<string, CatalogEnemyDefinitionSnapshot> _enemies = new()
    {
        ["enemy-shadow-v1"] = new CatalogEnemyDefinitionSnapshot(
            "enemy-shadow-v1", "1.0", "Ombre", "Une créature d'ombre.",
            null, "Fragile", null, "Common", null,
            2, 10, 0, 99, false, false, null,
            ["shadow", "common"],
            new CatalogEnemyStatBlockSnapshot(30, 8, 3, 0, 10, 5, 0, 0),
            [new CatalogEnemySkillSnapshot("skill.shadow.slash", "Lance d'ombre", "Damage", "SingleEnemy", "Damage", 0, 0, 12)]),

        ["enemy-phantom-v1"] = new CatalogEnemyDefinitionSnapshot(
            "enemy-phantom-v1", "1.0", "Fantôme", "Un esprit errant.",
            null, "Skirmisher", null, "Common", null,
            3, 8, 1, 99, false, false, null,
            ["phantom", "common"],
            new CatalogEnemyStatBlockSnapshot(25, 10, 2, 0, 12, 7, 0, 0),
            [new CatalogEnemySkillSnapshot("skill.phantom.touch", "Toucher spectral", "Damage", "SingleEnemy", "Damage", 0, 0, 14)]),

        ["enemy-elite-guardian-v1"] = new CatalogEnemyDefinitionSnapshot(
            "enemy-elite-guardian-v1", "1.0", "Gardien d'élite", "Un gardien renforcé.",
            null, "Guard", null, "Elite", null,
            5, 5, 2, 99, false, true, "rare",
            ["guardian", "elite"],
            new CatalogEnemyStatBlockSnapshot(50, 12, 8, 5, 6, 3, 0, 0),
            [
                new CatalogEnemySkillSnapshot("skill.guardian.smash", "Écrasement", "Damage", "SingleEnemy", "Damage", 0, 0, 18),
                new CatalogEnemySkillSnapshot("skill.guardian.shield", "Bouclier", "Defense", "Self", "Guard", 0, 0, 10),
            ]),

        ["enemy-boss-threshold-v1"] = new CatalogEnemyDefinitionSnapshot(
            "enemy-boss-threshold-v1", "1.0", "Gardien du Seuil", "Le boss du seuil.",
            null, "Bruiser", "Threshold", "Boss", null,
            8, 1, 0, 99, true, false, "room-boss",
            ["boss", "threshold"],
            new CatalogEnemyStatBlockSnapshot(80, 15, 10, 8, 8, 5, 0, 0),
            [
                new CatalogEnemySkillSnapshot("skill.boss.crush", "Écrasement", "Damage", "SingleEnemy", "Damage", 0, 0, 22),
                new CatalogEnemySkillSnapshot("skill.boss.roar", "Rugissement", "Debuff", "AllEnemies", "ApplyWeaken", 0, 0, 0),
            ]),
    };

    public void Register(CatalogEnemyDefinitionSnapshot enemy)
    {
        _enemies[enemy.Key] = enemy;
    }

    public Task<CatalogEnemyDefinitionSnapshot?> GetEnemyByKeyAsync(
        string enemyDefinitionKey,
        CancellationToken cancellationToken = default)
    {
        _enemies.TryGetValue(enemyDefinitionKey, out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<CatalogEnemyDefinitionSnapshot>> ListEligibleEnemiesAsync(
        EnemyEligibilityContext context,
        CancellationToken cancellationToken = default)
    {
        var eligible = _enemies.Values
            .Where(e => !e.IsBoss || context.NodeEventType == "RoomBoss")
            .Where(e => !e.IsElite || context.NodeEventType is "Elite" or "RoomBoss")
            .Where(e => context.RoomDepth >= e.MinDepth && context.RoomDepth <= e.MaxDepth)
            .Where(e => !context.ExcludedTags.Any() || !e.Tags.Any(t => context.ExcludedTags.Contains(t)))
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyCollection<CatalogEnemyDefinitionSnapshot>>(eligible);
    }
}
