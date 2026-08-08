using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class RoomStructuralProfileTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenOrganicCarvingAllowsSubRooms()
    {
        var act = () => new RoomStructuralProfile(CarvingStyle.Organic, subRoomsAllowed: true);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*Organic*sub-rooms*");
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenOrganicCarvingDisallowsSubRooms()
    {
        var act = () => new RoomStructuralProfile(CarvingStyle.Organic, subRoomsAllowed: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenRectangularCarvingAllowsSubRooms()
    {
        var act = () => new RoomStructuralProfile(CarvingStyle.Rectangular, subRoomsAllowed: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void Default_ShouldBeRectangularWithoutSubRooms()
    {
        RoomStructuralProfile.Default.CarvingStyle.Should().Be(CarvingStyle.Rectangular);
        RoomStructuralProfile.Default.SubRoomsAllowed.Should().BeFalse();
    }
}
