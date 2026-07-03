using FluentAssertions;
using Leds.Catalog.Application.RoomThemeAffinities.Dtos;
using Leds.Catalog.Application.RoomThemeAffinities.ListRoomThemeAffinities;
using Leds.Catalog.Application.RoomThemeAffinities.Ports;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.RoomThemeAffinities;

public sealed class ListRoomThemeAffinitiesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedAffinities_WhenAffinitiesExist()
    {
        var dto = new RoomThemeAffinityDto(Guid.NewGuid(), "Fear", "Terrifying", 2.5m);

        var readStore = Substitute.For<IRoomThemeAffinityReadStore>();
        readStore.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<RoomThemeAffinityDto>>([dto]));

        var handler = new ListRoomThemeAffinitiesQueryHandler(readStore);

        var response = await handler.Handle(new ListRoomThemeAffinitiesQuery(), CancellationToken.None);

        response.Affinities.Should().ContainSingle();
        var result = response.Affinities.Single();
        result.ThemeFrom.Should().Be("Fear");
        result.ThemeTo.Should().Be("Terrifying");
        result.Weight.Should().Be(2.5m);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoAffinitiesExist()
    {
        var readStore = Substitute.For<IRoomThemeAffinityReadStore>();
        readStore.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<RoomThemeAffinityDto>>(Array.Empty<RoomThemeAffinityDto>()));

        var handler = new ListRoomThemeAffinitiesQueryHandler(readStore);

        var response = await handler.Handle(new ListRoomThemeAffinitiesQuery(), CancellationToken.None);

        response.Affinities.Should().BeEmpty();
    }
}
