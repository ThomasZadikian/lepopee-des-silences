using FluentAssertions;
using Leds.Catalog.Domain.Skills.Definitions;
using Leds.Catalog.Infrastructure.ReadStores;

namespace Leds.Catalog.UnitTests.Application.Skills.Definitions;

public sealed class SkillDefinitionReadStoreTests
{
    private readonly InMemorySkillDefinitionReadStore _store = new();

    [Fact]
    public async Task ListActiveAsync_ShouldReturnAllSeeds()
    {
        var results = await _store.ListActiveAsync(CancellationToken.None);

        results.Should().HaveCount(5);
        results.Should().OnlyContain(d => d.IsActive);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnDefinition_WhenExists()
    {
        var result = await _store.GetByKeyAsync(
            "skill.basic.strike", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Key.Value.Should().Be("skill.basic.strike");
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnGuardSkillWithCurrentGuardEffect()
    {
        var result = await _store.GetByKeyAsync(
            "skill.basic.guard", CancellationToken.None);

        result.Should().NotBeNull();
        result!.EffectType.Should().Be("AddCurrentGuard");
        result.BasePower.Should().Be(5);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _store.GetByKeyAsync(
            "skill.unknown", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldBeCaseInsensitive()
    {
        var result = await _store.GetByKeyAsync(
            "SKILL.BASIC.STRIKE", CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ListByTypeAsync_ShouldReturnDamageSkills()
    {
        var results = await _store.ListByTypeAsync(
            "Damage", CancellationToken.None);

        results.Should().ContainSingle(d => d.Key.Value == "skill.basic.strike");
    }

    [Fact]
    public async Task ListByTypeAsync_ShouldReturnDebuffSkills()
    {
        var results = await _store.ListByTypeAsync(
            "Debuff", CancellationToken.None);

        results.Select(d => d.Key.Value)
            .Should()
            .Contain("skill.basic.weaken")
            .And.Contain("skill.basic.disrupt");
    }

    [Fact]
    public async Task ListByTypeAsync_ShouldReturnEmpty_WhenUnknownType()
    {
        var results = await _store.ListByTypeAsync(
            "Unknown", CancellationToken.None);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ListByTypeAsync_ShouldBeCaseInsensitive()
    {
        var results = await _store.ListByTypeAsync(
            "damage", CancellationToken.None);

        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListByKeysAsync_ShouldReturnMatching()
    {
        var results = await _store.ListByKeysAsync(
            ["skill.basic.strike", "skill.basic.guard"],
            CancellationToken.None);

        results.Select(d => d.Key.Value)
            .Should()
            .Contain("skill.basic.strike")
            .And.Contain("skill.basic.guard");
    }

    [Fact]
    public async Task ListByKeysAsync_ShouldIgnoreUnknownKeys()
    {
        var results = await _store.ListByKeysAsync(
            ["skill.basic.strike", "skill.unknown"],
            CancellationToken.None);

        results.Should().ContainSingle(d => d.Key.Value == "skill.basic.strike");
    }

    [Fact]
    public async Task ListByKeysAsync_ShouldReturnEmpty_WhenNullKeys()
    {
        var results = await _store.ListByKeysAsync(
            null!, CancellationToken.None);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ListByKeysAsync_ShouldReturnEmpty_WhenEmptyKeys()
    {
        var results = await _store.ListByKeysAsync(
            [], CancellationToken.None);

        results.Should().BeEmpty();
    }
}
