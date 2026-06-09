using FluentAssertions;
using Leds.Catalog.Infrastructure.ReadStores;

namespace Leds.Catalog.UnitTests.Infrastructure.ReadStores;

public sealed class InMemoryEnemyDefinitionReadStoreTests
{
    private readonly InMemoryEnemyDefinitionReadStore _readStore = new();

    [Fact]
    public async Task ListActiveAsync_ShouldReturnSeededEnemyDefinitions()
    {
        var definitions = await _readStore.ListActiveAsync(CancellationToken.None);

        definitions.Should().NotBeEmpty();
        definitions.Should().OnlyContain(d => d.IsActive);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnExpectedEnemy()
    {
        var definition = await _readStore.GetByKeyAsync(
            "enemy.threshold.doubt-fragment",
            CancellationToken.None);

        definition.Should().NotBeNull();
        definition!.Name.Value.Should().Be("Fragment de Doute");
        definition.Archetype.Should().Be("Fragile");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenUnknown()
    {
        var definition = await _readStore.GetByKeyAsync(
            "enemy.unknown",
            CancellationToken.None);

        definition.Should().BeNull();
    }

    [Fact]
    public async Task ListByRoomTypeAsync_ShouldReturnEnemiesCompatibleWithRoomType()
    {
        var definitions = await _readStore.ListByRoomTypeAsync(
            "Threshold",
            CancellationToken.None);

        definitions.Should().NotBeEmpty();
        definitions.Should().AllSatisfy(d =>
            d.CompatibleRoomTypes.Should().Contain("Threshold"));
    }

    [Fact]
    public async Task ListByRoomTypeAsync_ShouldBeCaseInsensitive()
    {
        var definitions = await _readStore.ListByRoomTypeAsync(
            "THRESHOLD",
            CancellationToken.None);

        definitions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListCompatibleAsync_ShouldFilterByRoomTypeAndRiskLevel()
    {
        var definitions = await _readStore.ListCompatibleAsync(
            "Threshold", 1,
            CancellationToken.None);

        definitions.Should().NotBeEmpty();
        definitions.Should().OnlyContain(d =>
            d.MinRiskLevel <= 1 && 1 <= d.MaxRiskLevel);
    }

    [Fact]
    public async Task ListCompatibleAsync_ShouldReturnEmpty_WhenRiskLevelOutOfRange()
    {
        var definitions = await _readStore.ListCompatibleAsync(
            "Threshold", 99,
            CancellationToken.None);

        definitions.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedData_ShouldContainAtLeastOneEnemyPerRoomType()
    {
        var roomTypes = new[] { "Threshold", "Forest", "Rupture", "Silence", "Memory", "Antechamber", "Final" };

        foreach (var roomType in roomTypes)
        {
            var definitions = await _readStore.ListByRoomTypeAsync(roomType, CancellationToken.None);
            definitions.Should().NotBeEmpty(
                $"{roomType} should have at least one enemy definition");
        }
    }

    [Fact]
    public async Task SeedData_ShouldContainFinalEnemies()
    {
        var definitions = await _readStore.ListByRoomTypeAsync("Final", CancellationToken.None);

        definitions.Select(d => d.Key.Value)
            .Should()
            .Contain("enemy.final.silent-double")
            .And.Contain("enemy.final.last-echo");
    }

    [Fact]
    public async Task SeedData_ShouldHaveAll14Seeds()
    {
        var definitions = await _readStore.ListActiveAsync(CancellationToken.None);

        definitions.Should().HaveCount(14);
    }
}
