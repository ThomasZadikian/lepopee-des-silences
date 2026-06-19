using FluentAssertions;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogEnemyProviderTests
{
    [Fact]
    public async Task CatalogGateway_ShouldReturnSeedEnemy()
    {
        var gateway = new StubCatalogContentGateway();

        var enemy = await gateway.GetEnemyDefinitionByKeyAsync("enemy.threshold.echo");

        enemy.Should().NotBeNull();
        enemy!.Key.Should().Be("enemy.threshold.echo");
        enemy.DisplayName.Should().Be("Écho");
        enemy.Archetype.Should().Be("Fragile");
        enemy.CompatibleRoomTypes.Should().Contain("Threshold");
    }

    [Fact]
    public async Task CatalogGateway_ShouldReturnNull_ForUnknownKey()
    {
        var gateway = new StubCatalogContentGateway();

        var enemy = await gateway.GetEnemyDefinitionByKeyAsync("enemy-nonexistent");

        enemy.Should().BeNull();
    }

    [Fact]
    public async Task CatalogGateway_ShouldReturnDifficultyRange()
    {
        var gateway = new StubCatalogContentGateway();

        var enemy = await gateway.GetEnemyDefinitionByKeyAsync("enemy.threshold.echo");

        enemy!.BaseDifficulty.Should().Be(1);
        enemy.MinRiskLevel.Should().Be(1);
        enemy.MaxRiskLevel.Should().Be(2);
    }

    [Fact]
    public async Task CatalogGateway_ShouldReturnSkills()
    {
        var gateway = new StubCatalogContentGateway();

        var enemy = await gateway.GetEnemyDefinitionByKeyAsync("enemy.threshold.echo");

        enemy!.SkillKeys.Should().ContainSingle("skill.basic.strike");
    }

    [Fact]
    public async Task CatalogGateway_ShouldListCompatibleEnemies()
    {
        var gateway = new StubCatalogContentGateway();

        var enemies = await gateway.ListCompatibleEnemyDefinitionsAsync("Threshold", 3);

        enemies.Should().NotBeEmpty();
        enemies.Should().OnlyContain(e => e.CompatibleRoomTypes.Contains("Threshold"));
        enemies.Should().OnlyContain(e => e.MinRiskLevel <= 3 && 3 <= e.MaxRiskLevel);
    }

    [Fact]
    public async Task CatalogGateway_ShouldIncludeElite_ForFinalHighRisk()
    {
        var gateway = new StubCatalogContentGateway();

        var enemies = await gateway.ListCompatibleEnemyDefinitionsAsync("Final", 5);

        enemies.Should().Contain(e => e.Archetype == "Elite");
    }

    [Fact]
    public async Task CatalogGateway_ShouldIncludeBoss_ForRoomBossRisk()
    {
        var gateway = new StubCatalogContentGateway();

        var enemies = await gateway.ListCompatibleEnemyDefinitionsAsync("Threshold", 5);

        enemies.Should().Contain(e => e.Archetype == "Boss");
    }

    [Fact]
    public async Task CatalogGateway_ShouldListEnemiesByRoomType()
    {
        var gateway = new StubCatalogContentGateway();

        var enemies = await gateway.ListEnemyDefinitionsByRoomTypeAsync("Threshold");

        enemies.Should().NotBeEmpty();
        enemies.Should().Contain(e => e.Key == "enemy.threshold.doubt-fragment");
    }

    [Fact]
    public async Task CatalogGateway_ShouldReturnRoomBossProfile()
    {
        var gateway = new StubCatalogContentGateway();

        var boss = await gateway.GetRoomBossProfileAsync("Threshold");

        boss.Should().NotBeNull();
        boss!.Key.Should().Be("boss.threshold.warden");
    }

    [Fact]
    public async Task CatalogGateway_ShouldReturnNull_ForUnknownRoomBossProfile()
    {
        var gateway = new StubCatalogContentGateway();

        var boss = await gateway.GetRoomBossProfileAsync("Unknown");

        boss.Should().BeNull();
    }
}
