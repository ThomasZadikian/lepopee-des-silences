using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class GridRoomLayoutTemplateProviderTests
{
    private const string GeneratorVersion = "grid-room-layout-1.0.0";

    private static IGridRoomLayoutTemplateProvider CreateSut()
        => new GridRoomLayoutTemplateProvider();

    [Fact]
    public void GetTemplate_ShouldReturnTheDefaultTemplate_WhenNoCatalogRoomKeyIsGiven()
    {
        var template = CreateSut().GetTemplate(RoomType.Threshold, GeneratorVersion);

        template.Key.Should().Be("tactical-default-v1");
    }

    [Theory]
    [InlineData("room.jardin", "room.jardin-v1", 26, 18)]
    [InlineData("room.hopital", "room.hopital-v1", 26, 16)]
    [InlineData("room.enfer3", "room.enfer3-v1", 24, 18)]
    [InlineData("room.cavernedecrystal", "room.cavernedecrystal-v1", 24, 18)]
    [InlineData("room.labyrinthe", "room.labyrinthe-v1", 26, 18)]
    public void GetTemplate_ShouldReturnTheRoomSpecificTemplate_WhenCatalogRoomKeyMatches(
        string catalogRoomKey, string expectedTemplateKey, int expectedWidth, int expectedHeight)
    {
        var template = CreateSut().GetTemplate(RoomType.Threshold, GeneratorVersion, catalogRoomKey);

        template.Key.Should().Be(expectedTemplateKey);
        template.Width.Should().Be(expectedWidth);
        template.Height.Should().Be(expectedHeight);
        template.Width.Should().BeLessThanOrEqualTo(35, because: "the agreed grid ceiling for this integration is 35x25");
        template.Height.Should().BeLessThanOrEqualTo(25, because: "the agreed grid ceiling for this integration is 35x25");
    }

    [Fact]
    public void GetTemplate_ShouldFallBackToDefault_WhenCatalogRoomKeyIsUnprofiled()
    {
        var template = CreateSut().GetTemplate(RoomType.Threshold, GeneratorVersion, "room.some-unprofiled-room");

        template.Key.Should().Be("tactical-default-v1");
    }
}
