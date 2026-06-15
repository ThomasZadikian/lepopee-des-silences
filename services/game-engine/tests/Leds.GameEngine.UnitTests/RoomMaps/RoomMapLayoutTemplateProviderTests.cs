using FluentAssertions;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class RoomMapLayoutTemplateProviderTests
{
    [Fact]
    public void GetTemplate_ShouldReturnDefaultThresholdTemplate_WhenRoomTypeIsThresholdAndVersionMatches()
    {
        var provider = new RoomMapLayoutTemplateProvider();

        var template = provider.GetTemplate(
            RoomType.Threshold,
            DefaultRoomMapLayoutTemplates.GeneratorVersion);

        template.Should().NotBeNull();
        template.Key.Should().Be("threshold-default-v1");
        template.Version.Should().Be(DefaultRoomMapLayoutTemplates.GeneratorVersion);
        template.RoomType.Should().Be(RoomType.Threshold);
        template.RowNodeCounts.Should().Equal(2, 3, 4, 3, 4, 3, 2, 1);
        template.TotalNodeCount.Should().Be(22);
        template.InitialRowIndex.Should().Be(0);
        template.BossRowIndex.Should().Be(7);
    }

    [Fact]
    public void GetTemplate_ShouldReturnDefaultTemplate_WhenGeneratorVersionIsDefault()
    {
        var provider = new RoomMapLayoutTemplateProvider();

        var template = provider.GetTemplate(
            RoomType.Threshold,
            DefaultRoomMapLayoutTemplates.GeneratorVersion);

        template.Should().NotBeNull();
        template.RoomType.Should().Be(RoomType.Threshold);
    }

    [Fact]
    public void GetTemplate_ShouldThrow_WhenNoTemplateFoundForRoomType()
    {
        var provider = new RoomMapLayoutTemplateProvider();

        var act = () => provider.GetTemplate(RoomType.Final, "unknown-version");

        act.Should().Throw<KeyNotFoundException>();
    }
}