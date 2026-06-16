using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Infrastructure.Catalog;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogEnemyProviderTests
{
    [Fact]
    public async Task EnemyProvider_ShouldReturnSeedEnemy()
    {
        var provider = new InMemoryCatalogEnemyDefinitionProvider();

        var enemy = await provider.GetEnemyByKeyAsync("enemy-shadow-v1");

        enemy.Should().NotBeNull();
        enemy!.Key.Should().Be("enemy-shadow-v1");
        enemy.DisplayName.Should().Be("Ombre");
        enemy.Archetype.Should().Be("Fragile");
        enemy.Rank.Should().Be("Common");
    }

    [Fact]
    public async Task EnemyProvider_ShouldReturnNull_ForUnknownKey()
    {
        var provider = new InMemoryCatalogEnemyDefinitionProvider();

        var enemy = await provider.GetEnemyByKeyAsync("enemy-nonexistent");

        enemy.Should().BeNull();
    }

    [Fact]
    public async Task EnemyProvider_ShouldReturnStatBlock()
    {
        var provider = new InMemoryCatalogEnemyDefinitionProvider();

        var enemy = await provider.GetEnemyByKeyAsync("enemy-shadow-v1");

        enemy!.StatBlock.Should().NotBeNull();
        enemy.StatBlock.MaxVitality.Should().Be(30);
        enemy.StatBlock.AttackPower.Should().Be(8);
        enemy.StatBlock.Defense.Should().Be(3);
        enemy.StatBlock.Speed.Should().Be(10);
    }

    [Fact]
    public async Task EnemyProvider_ShouldReturnSkills()
    {
        var provider = new InMemoryCatalogEnemyDefinitionProvider();

        var enemy = await provider.GetEnemyByKeyAsync("enemy-shadow-v1");

        enemy!.Skills.Should().HaveCount(1);
        enemy.Skills.First().SkillDefinitionKey.Should().Be("skill.shadow.slash");
    }

    [Fact]
    public async Task EnemyProvider_ShouldListEligibleEnemies()
    {
        var provider = new InMemoryCatalogEnemyDefinitionProvider();
        var context = new EnemyEligibilityContext(
            "Threshold", null, 1, "Combat", 3, null, [], [], [], []);

        var enemies = await provider.ListEligibleEnemiesAsync(context);

        enemies.Should().NotBeEmpty();
        enemies.Should().AllSatisfy(e => e.IsBoss.Should().BeFalse());
    }

    [Fact]
    public async Task EnemyProvider_ShouldIncludeElite_ForEliteNodeType()
    {
        var provider = new InMemoryCatalogEnemyDefinitionProvider();
        var context = new EnemyEligibilityContext(
            "Threshold", null, 2, "Elite", 5, null, [], [], [], []);

        var enemies = await provider.ListEligibleEnemiesAsync(context);

        enemies.Should().Contain(e => e.IsElite);
    }

    [Fact]
    public async Task EnemyProvider_ShouldIncludeBoss_ForRoomBossNodeType()
    {
        var provider = new InMemoryCatalogEnemyDefinitionProvider();
        var context = new EnemyEligibilityContext(
            "Threshold", null, 0, "RoomBoss", 5, null, [], [], [], []);

        var enemies = await provider.ListEligibleEnemiesAsync(context);

        enemies.Should().Contain(e => e.IsBoss);
    }

    [Fact]
    public async Task RoomEnemyPoolProvider_ShouldReturnSeedPool()
    {
        var provider = new InMemoryCatalogRoomEnemyPoolProvider();

        var pool = await provider.GetEnemyPoolAsync("pool.threshold.default");

        pool.Should().NotBeNull();
        pool!.Key.Should().Be("pool.threshold.default");
        pool.Entries.Should().HaveCount(3);
    }

    [Fact]
    public async Task RoomEnemyPoolProvider_ShouldReturnPoolForRoom()
    {
        var provider = new InMemoryCatalogRoomEnemyPoolProvider();
        var context = new RoomEnemyPoolEligibilityContext("Threshold", null, 1, "Combat", 3);

        var pool = await provider.GetEnemyPoolForRoomAsync(context);

        pool.Should().NotBeNull();
    }

    [Fact]
    public async Task RoomEnemyPoolProvider_ShouldReturnNull_ForUnknownPool()
    {
        var provider = new InMemoryCatalogRoomEnemyPoolProvider();

        var pool = await provider.GetEnemyPoolAsync("pool.nonexistent");

        pool.Should().BeNull();
    }
}
