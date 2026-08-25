using FluentAssertions;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class CatalogMarkovRoomTypeResolverTests
{
    [Fact]
    public async Task ResolveNextRoomTypeKeyAsync_ShouldNotCreatePeriodicHimLitRoomAtDepthTen()
    {
        var resolver = new CatalogMarkovRoomTypeResolver(new StubCatalogContentGateway());

        var result = await resolver.ResolveNextRoomTypeKeyAsync("seed", 10, "Memory");

        result.Should().NotBe(CatalogMarkovRoomTypeResolver.FinalTheme,
            because: "Him'Lit must be authored through mastery progression, not a universal floor cadence");
    }

    [Fact]
    public async Task ResolveNextRoomTypeKeyAsync_ShouldKeepThresholdAtDepthZero()
    {
        var resolver = new CatalogMarkovRoomTypeResolver(new StubCatalogContentGateway());

        var result = await resolver.ResolveNextRoomTypeKeyAsync("seed", 0, "Memory");

        result.Should().Be(CatalogMarkovRoomTypeResolver.ThresholdTheme);
    }
}
