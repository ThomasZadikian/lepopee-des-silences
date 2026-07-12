using FluentAssertions;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Generation;

/// <summary>
/// Mina's legendary "Protection de Him'Lit" tightens the boss-recurrence interval
/// (see DeterministicRunGenerator) — this covers the resolver's side of that contract.
/// </summary>
public sealed class CatalogMarkovRoomTypeResolverTests
{
    [Fact]
    public async Task ResolveNextRoomTypeKeyAsync_ShouldUseDefaultBossInterval_WhenNotOverridden()
    {
        var resolver = new CatalogMarkovRoomTypeResolver(new StubCatalogContentGateway());

        var atTen = await resolver.ResolveNextRoomTypeKeyAsync("seed", 10, "Memory");
        var atSeven = await resolver.ResolveNextRoomTypeKeyAsync("seed", 7, "Memory");

        atTen.Should().Be(CatalogMarkovRoomTypeResolver.FinalTheme);
        atSeven.Should().NotBe(CatalogMarkovRoomTypeResolver.FinalTheme);
    }

    [Fact]
    public async Task ResolveNextRoomTypeKeyAsync_ShouldUseCustomBossInterval_WhenProvided()
    {
        var resolver = new CatalogMarkovRoomTypeResolver(new StubCatalogContentGateway());

        var atSeven = await resolver.ResolveNextRoomTypeKeyAsync(
            "seed", 7, "Memory", cancellationToken: default, bossInterval: 7);
        var atTen = await resolver.ResolveNextRoomTypeKeyAsync(
            "seed", 10, "Memory", cancellationToken: default, bossInterval: 7);

        atSeven.Should().Be(CatalogMarkovRoomTypeResolver.FinalTheme,
            because: "a tightened interval (Mina's Protection de Him'Lit) must recur Him'Lit sooner.");
        atTen.Should().NotBe(CatalogMarkovRoomTypeResolver.FinalTheme,
            because: "10 is not a multiple of the overridden interval (7).");
    }

    [Fact]
    public async Task ResolveNextRoomTypeKeyAsync_ShouldFallBackToDefaultInterval_WhenGivenZeroOrNegative()
    {
        var resolver = new CatalogMarkovRoomTypeResolver(new StubCatalogContentGateway());

        var result = await resolver.ResolveNextRoomTypeKeyAsync(
            "seed", 10, "Memory", cancellationToken: default, bossInterval: 0);

        result.Should().Be(CatalogMarkovRoomTypeResolver.FinalTheme);
    }
}
