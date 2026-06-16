using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Selection;
using Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class DeterministicEncounterEnemySelectorTests
{
    private static DeterministicEncounterEnemySelector CreateSelector() =>
        new(new DeterministicWeightedSelector());

    private static CatalogEnemyDefinitionSnapshot CreateEnemy(
        string key, string rank = "Common", bool isBoss = false, bool isElite = false,
        int weight = 10, int minDepth = 0, int maxDepth = 99, string archetype = "Fragile") =>
        new(key, "1.0", key, "", null, archetype, null, rank, null,
            3, weight, minDepth, maxDepth, isBoss, isElite, null, [],
            new CatalogEnemyStatBlockSnapshot(30, 8, 3, 0, 10, 5, 0, 0),
            [new CatalogEnemySkillSnapshot($"{key}.skill", "Hit", "Damage", "SingleEnemy", "Damage", 0, 0, 10)]);

    private static EncounterEnemySelectionContext CreateContext(
        string seed = "test-seed", string nodeEventType = "Combat", int riskLevel = 3) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), seed,
            "Threshold", null, 1, nodeEventType, riskLevel, 1.0m, [], []);

    [Fact]
    public void SelectEnemies_ShouldReturnEmpty_WhenNoCandidates()
    {
        var selector = CreateSelector();
        var result = selector.SelectEnemies(CreateContext(), []);
        result.Should().BeEmpty();
    }

    [Fact]
    public void SelectEnemies_ShouldFilterOutBosses_ForCombatNode()
    {
        var selector = CreateSelector();
        var candidates = new[]
        {
            CreateEnemy("common1"),
            CreateEnemy("boss1", isBoss: true, rank: "Boss"),
        };

        var result = selector.SelectEnemies(CreateContext(), candidates);

        result.Should().AllSatisfy(s => s.Enemy.IsBoss.Should().BeFalse());
    }

    [Fact]
    public void SelectEnemies_ShouldFilterOutElites_ForCombatNode()
    {
        var selector = CreateSelector();
        var candidates = new[]
        {
            CreateEnemy("common1"),
            CreateEnemy("elite1", isElite: true, rank: "Elite"),
        };

        var result = selector.SelectEnemies(CreateContext(), candidates);

        result.Should().AllSatisfy(s => s.Enemy.IsElite.Should().BeFalse());
    }

    [Fact]
    public void SelectEnemies_ShouldPreferElite_ForEliteNode()
    {
        var selector = CreateSelector();
        var candidates = new[]
        {
            CreateEnemy("common1"),
            CreateEnemy("elite1", isElite: true, rank: "Elite"),
        };

        var result = selector.SelectEnemies(CreateContext(nodeEventType: "Elite"), candidates);

        result.Should().HaveCount(1);
        result.First().Enemy.Key.Should().Be("elite1");
    }

    [Fact]
    public void SelectEnemies_ShouldPreferBoss_ForBossNode()
    {
        var selector = CreateSelector();
        var candidates = new[]
        {
            CreateEnemy("common1"),
            CreateEnemy("boss1", isBoss: true, rank: "Boss"),
        };

        var result = selector.SelectEnemies(CreateContext(nodeEventType: "RoomBoss"), candidates);

        result.Should().HaveCount(1);
        result.First().Enemy.Key.Should().Be("boss1");
    }

    [Fact]
    public void SelectEnemies_ShouldBeDeterministic_ForSameSeed()
    {
        var selector = CreateSelector();
        var candidates = new[]
        {
            CreateEnemy("enemy1", weight: 10),
            CreateEnemy("enemy2", weight: 5),
            CreateEnemy("enemy3", weight: 3),
        };
        var context = CreateContext(seed: "deterministic-seed");

        var result1 = selector.SelectEnemies(context, candidates);
        var result2 = selector.SelectEnemies(context, candidates);

        result1.Select(r => r.Enemy.Key).Should().Equal(result2.Select(r => r.Enemy.Key));
    }

    [Fact]
    public void SelectEnemies_ShouldUseDifficultyMultiplier()
    {
        var selector = CreateSelector();
        var candidates = new[] { CreateEnemy("enemy1") };
        var context = CreateContext() with { DifficultyMultiplier = 1.5m };

        var result = selector.SelectEnemies(context, candidates);

        result.Should().HaveCount(1);
        result.First().DifficultyMultiplier.Should().Be(1.5m);
    }

    [Fact]
    public void SelectEnemies_ShouldRespectMaxEnemyCount_ForHighRisk()
    {
        var selector = CreateSelector();
        var candidates = Enumerable.Range(0, 10)
            .Select(i => CreateEnemy($"enemy{i}"))
            .ToArray();
        var context = CreateContext(riskLevel: 5);

        var result = selector.SelectEnemies(context, candidates);

        result.Count.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public void SelectEnemies_ShouldReturnSingle_ForEliteNode()
    {
        var selector = CreateSelector();
        var candidates = new[]
        {
            CreateEnemy("elite1", isElite: true, rank: "Elite"),
            CreateEnemy("elite2", isElite: true, rank: "Elite"),
        };
        var context = CreateContext(nodeEventType: "Elite");

        var result = selector.SelectEnemies(context, candidates);

        result.Should().HaveCount(1);
    }

    [Fact]
    public void SelectEnemies_ShouldFallbackToAll_WhenNoMatchForNodeType()
    {
        var selector = CreateSelector();
        var candidates = new[]
        {
            CreateEnemy("common1"),
            CreateEnemy("common2"),
        };
        var context = CreateContext(nodeEventType: "Elite");

        var result = selector.SelectEnemies(context, candidates);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public void SelectEnemies_ShouldSetDifficultyMultiplier_OnSelectedEnemies()
    {
        var selector = CreateSelector();
        var candidates = new[] { CreateEnemy("enemy1") };
        var context = CreateContext() with { DifficultyMultiplier = 2.0m };

        var result = selector.SelectEnemies(context, candidates);

        result.First().DifficultyMultiplier.Should().Be(2.0m);
    }

    [Fact]
    public void SelectEnemies_ShouldNotExceedCandidateCount()
    {
        var selector = CreateSelector();
        var candidates = new[] { CreateEnemy("only-one") };
        var context = CreateContext(riskLevel: 5);

        var result = selector.SelectEnemies(context, candidates);

        result.Should().HaveCount(1);
    }
}
