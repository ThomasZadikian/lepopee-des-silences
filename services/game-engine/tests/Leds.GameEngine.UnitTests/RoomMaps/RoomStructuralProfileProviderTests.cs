using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class RoomStructuralProfileProviderTests
{
    private static IRoomStructuralProfileProvider CreateSut()
        => new HardcodedRoomStructuralProfileProvider();

    [Theory]
    [InlineData("room.jardin", CarvingStyle.Organic, false)]
    [InlineData("room.cavernedecrystal", CarvingStyle.Organic, false)]
    [InlineData("room.hopital", CarvingStyle.Rectangular, true)]
    [InlineData("room.enfer3", CarvingStyle.Rectangular, true)]
    [InlineData("room.labyrinthe", CarvingStyle.Rectangular, true)]
    public void GetProfile_ShouldReturnTheProfiledTypology(
        string catalogRoomKey, CarvingStyle expectedStyle, bool expectedSubRoomsAllowed)
    {
        var profile = CreateSut().GetProfile(RoomType.Threshold, catalogRoomKey);

        profile.CarvingStyle.Should().Be(expectedStyle);
        profile.SubRoomsAllowed.Should().Be(expectedSubRoomsAllowed);
    }

    [Fact]
    public void GetProfile_ShouldReturnDefault_WhenCatalogRoomKeyIsUnprofiled()
    {
        var profile = CreateSut().GetProfile(RoomType.Forest, "room.some-unprofiled-room");

        profile.Should().Be(RoomStructuralProfile.Default);
    }

    [Fact]
    public void GetProfile_ShouldReturnDefault_WhenCatalogRoomKeyIsNull()
    {
        var profile = CreateSut().GetProfile(RoomType.Forest, catalogRoomKey: null);

        profile.Should().Be(RoomStructuralProfile.Default);
    }

    [Fact]
    public void GetProfile_ShouldBeCaseInsensitiveOnCatalogRoomKey()
    {
        var profile = CreateSut().GetProfile(RoomType.Threshold, "ROOM.JARDIN");

        profile.CarvingStyle.Should().Be(CarvingStyle.Organic);
    }
}
