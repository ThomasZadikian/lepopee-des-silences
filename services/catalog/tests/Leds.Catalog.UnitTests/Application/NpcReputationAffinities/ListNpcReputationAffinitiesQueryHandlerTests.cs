using FluentAssertions;
using Leds.Catalog.Application.NpcReputationAffinities.Dtos;
using Leds.Catalog.Application.NpcReputationAffinities.ListNpcReputationAffinities;
using Leds.Catalog.Application.NpcReputationAffinities.Ports;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.NpcReputationAffinities;

public sealed class ListNpcReputationAffinitiesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedAffinities_WhenAffinitiesExist()
    {
        var dto = new NpcReputationAffinityDto(Guid.NewGuid(), "npc.hitomi", "npc.elyas", 0.5m);

        var readStore = Substitute.For<INpcReputationAffinityReadStore>();
        readStore.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<NpcReputationAffinityDto>>([dto]));

        var handler = new ListNpcReputationAffinitiesQueryHandler(readStore);

        var response = await handler.Handle(new ListNpcReputationAffinitiesQuery(), CancellationToken.None);

        response.Affinities.Should().ContainSingle();
        var result = response.Affinities.Single();
        result.NpcKeyFrom.Should().Be("npc.hitomi");
        result.NpcKeyTo.Should().Be("npc.elyas");
        result.Weight.Should().Be(0.5m);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoAffinitiesExist()
    {
        var readStore = Substitute.For<INpcReputationAffinityReadStore>();
        readStore.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<NpcReputationAffinityDto>>(Array.Empty<NpcReputationAffinityDto>()));

        var handler = new ListNpcReputationAffinitiesQueryHandler(readStore);

        var response = await handler.Handle(new ListNpcReputationAffinitiesQuery(), CancellationToken.None);

        response.Affinities.Should().BeEmpty();
    }
}
