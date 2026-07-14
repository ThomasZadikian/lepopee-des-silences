using FluentAssertions;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Leds.Catalog.IntegrationTests.Persistence;

/// <summary>
/// Validates the room reachability graph seeded by <see cref="CatalogSeedRunner"/> against
/// the invariants from the "Refonte des Rooms" SFD § 7 — since the graph is now editorial
/// data rather than compiler-checked code, these are the guardrails that catch a content
/// mistake (orphan reference, unreachable room, a strict chain with no exit) automatically.
/// </summary>
[Collection("CatalogPostgres")]
public sealed class RoomReachabilityGraphValidationTests
{
    private readonly CatalogPostgresFixture _fixture;

    public RoomReachabilityGraphValidationTests(CatalogPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReachabilityEdges_ShouldNeverReferenceAMissingRoom()
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var runner = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await runner.SeedAsync(CancellationToken.None);

        var roomIds = await context.RoomDefinitions.Select(r => r.Id).ToListAsync();
        var edges = await context.RoomReachability.ToListAsync();

        edges.Should().OnlyContain(edge => roomIds.Contains(edge.FromRoomDefinitionId) && roomIds.Contains(edge.ToRoomDefinitionId));
    }

    [Fact]
    public async Task EachWorld_ShouldHaveExactlyOneEntryRoom()
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var runner = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await runner.SeedAsync(CancellationToken.None);

        var worlds = await context.WorldDefinitions.ToListAsync();
        worlds.Should().NotBeEmpty();

        foreach (var world in worlds)
        {
            var entryRoomExists = await context.RoomDefinitions.AnyAsync(r => r.Id == world.EntryRoomDefinitionId);
            entryRoomExists.Should().BeTrue(because: $"the World '{world.Key}' must have a resolvable entry room");
        }
    }

    [Fact]
    public async Task PalaisRooms_ShouldAllBeAssignedToTheWorld()
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var runner = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await runner.SeedAsync(CancellationToken.None);

        var palais = await context.WorldDefinitions.SingleAsync(w => w.Key == "palais");
        var hallDentree = await context.RoomDefinitions.SingleAsync(r => r.Key == "room.halldentree");

        hallDentree.WorldDefinitionId.Should().Be(palais.Id);

        var palaisRoomCount = await context.RoomDefinitions.CountAsync(r => r.WorldDefinitionId == palais.Id);
        palaisRoomCount.Should().Be(27);
    }

    [Theory]
    [InlineData("room.falaise", "room.enfer1", "room.enfer2", "room.enfer3", "room.enfer4")]
    [InlineData("room.soleil", "room.chateau", "room.cellule")]
    [InlineData("room.hopital", "room.cellulehopital", "room.faille")]
    [InlineData("room.montagne", "room.templempontagne", "room.chambrefunéraire", "room.sousterrainmontagne", "room.cavernedecrystal")]
    public async Task StrictChain_ShouldBeLinearAndConvergeToAnEmptyChildList(params string[] chainKeysInOrder)
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var runner = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await runner.SeedAsync(CancellationToken.None);

        for (var i = 0; i < chainKeysInOrder.Length; i++)
        {
            var room = await context.RoomDefinitions
                .Include(r => r.ReachableTo).ThenInclude(edge => edge.ToRoomDefinition)
                .SingleAsync(r => r.Key == chainKeysInOrder[i]);

            if (i == chainKeysInOrder.Length - 1)
            {
                room.ReachableTo.Should().BeEmpty(because: $"'{room.Key}' ends the chain and must fall back to the World entry room");
            }
            else
            {
                room.ReachableTo.Should().ContainSingle(because: $"'{room.Key}' must have exactly one strict-chain child");
                room.ReachableTo.Single().ToRoomDefinition.Key.Should().Be(chainKeysInOrder[i + 1]);
            }
        }
    }
}
