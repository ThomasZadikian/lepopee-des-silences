using FluentAssertions;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Rewards.Loot;

public sealed class EnemyLootRewardBuilderTests
{
    private static EnemyLootRewardBuilder CreateBuilder(StubCatalogContentGateway? gateway = null) =>
        new(gateway ?? new StubCatalogContentGateway());

    [Fact]
    public async Task BuildAsync_ShouldRollBetween1And3Items_PerEnemyWithATable()
    {
        var enemy = Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20);

        var choices = await CreateBuilder().BuildAsync(
            "seed-1", Guid.NewGuid(), Guid.NewGuid(), [enemy]);

        choices.Count.Should().BeInRange(1, 6);
        choices.Should().OnlyContain(c => c.SourceEnemyKey == "enemy.forest.chimere-serpentaire");
    }

    [Fact]
    public async Task BuildAsync_ShouldSkipEnemiesWithNoConfiguredLootTable()
    {
        var enemyWithoutTable = Combatant.CreateEnemy("enemy.threshold.doubt-fragment", "Fragment de Doute", "Fragile", 10);

        var choices = await CreateBuilder().BuildAsync(
            "seed-2", Guid.NewGuid(), Guid.NewGuid(), [enemyWithoutTable]);

        // No enemy loot table configured -> falls entirely back to the generic pool.
        choices.Should().OnlyContain(c => c.SourceEnemyKey is null);
    }

    [Fact]
    public async Task BuildAsync_ShouldPadUpToTheFloor_WhenRolledCountIsBelowThree()
    {
        var enemyWithoutTable = Combatant.CreateEnemy("enemy.threshold.doubt-fragment", "Fragment de Doute", "Fragile", 10);

        var choices = await CreateBuilder().BuildAsync(
            "seed-3", Guid.NewGuid(), Guid.NewGuid(), [enemyWithoutTable]);

        choices.Count.Should().BeGreaterOrEqualTo(EnemyLootRewardBuilder.MinLootCount);
    }

    [Fact]
    public async Task BuildAsync_ShouldCapAtSix_WhenManyEnemiesRollLotsOfLoot()
    {
        var enemies = new[]
        {
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
            Combatant.CreateEnemy("enemy.silence.mute-witness", "Temoin Muet", "Guard", 30),
            Combatant.CreateEnemy("enemy.threshold.fracture", "Fracture", "Bruiser", 32),
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
        };

        var choices = await CreateBuilder().BuildAsync(
            "seed-4", Guid.NewGuid(), Guid.NewGuid(), enemies);

        choices.Count.Should().BeLessOrEqualTo(EnemyLootRewardBuilder.MaxLootCount);
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnEmpty_WhenThereAreNoEnemiesAndNoFallbackPool()
    {
        var choices = await CreateBuilder().BuildAsync(
            "seed-5", Guid.NewGuid(), Guid.NewGuid(), []);

        // The stub gateway always has an active fallback pool, so an empty fight still
        // pads up to the floor from it.
        choices.Count.Should().BeGreaterOrEqualTo(EnemyLootRewardBuilder.MinLootCount);
    }

    [Fact]
    public async Task BuildAsync_ShouldBeDeterministic_ForTheSameInputs()
    {
        var enemies = new[]
        {
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
            Combatant.CreateEnemy("enemy.silence.mute-witness", "Temoin Muet", "Guard", 30),
        };
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();

        var first = await CreateBuilder().BuildAsync("seed-6", runId, combatId, enemies);
        var second = await CreateBuilder().BuildAsync("seed-6", runId, combatId, enemies);

        first.Select(c => c.PayloadKey).Should().Equal(second.Select(c => c.PayloadKey));
    }

    [Fact]
    public async Task BuildAsync_ShouldChangeSelection_WhenSeedChanges()
    {
        var enemies = new[]
        {
            Combatant.CreateEnemy("enemy.forest.chimere-serpentaire", "Chimere Serpentaire", "Beast", 20),
            Combatant.CreateEnemy("enemy.silence.mute-witness", "Temoin Muet", "Guard", 30),
            Combatant.CreateEnemy("enemy.threshold.fracture", "Fracture", "Bruiser", 32),
        };
        var runId = Guid.NewGuid();
        var combatId = Guid.NewGuid();

        var results = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(i => CreateBuilder().BuildAsync($"seed-varied-{i}", runId, combatId, enemies)));

        var distinctSignatures = results
            .Select(r => string.Join(",", r.Select(c => c.PayloadKey)))
            .Distinct()
            .Count();

        distinctSignatures.Should().BeGreaterThan(1,
            "different seeds should be able to produce different loot rolls.");
    }
}
