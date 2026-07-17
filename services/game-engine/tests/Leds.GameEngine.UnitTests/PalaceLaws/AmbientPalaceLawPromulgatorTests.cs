using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Selection;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// <see cref="AmbientPalaceLawPromulgator"/> — the ambient promulgation trigger wired into
/// room transitions (<see cref="Application.Runs.MoveToNextRoom.MoveToNextRoomCommandHandler"/>),
/// replacing the retired player-chosen "Loi" map node.
/// </summary>
public sealed class AmbientPalaceLawPromulgatorTests
{
    private static PalaceLawDefinitionSnapshot CreateLaw(
        string key,
        bool isMajeure = false,
        string? roomKey = null,
        int baseWeight = 1,
        string polarity = "Neutre") => new(
        Key: key,
        Name: $"Loi {key}",
        Description: "desc",
        Version: "1.0.0",
        Status: "Active",
        Visibility: "Public",
        Priority: 0,
        ImpactDomains: ["Narrative"],
        BaseWeight: baseWeight,
        Rarity: "Commun",
        Polarity: polarity,
        IsMajeure: isMajeure,
        RoomKey: roomKey,
        IsCumulExempt: roomKey is not null);

    private static Mock<ICatalogContentGateway> CreateGateway(params PalaceLawDefinitionSnapshot[] laws)
    {
        var gateway = new Mock<ICatalogContentGateway>();
        gateway
            .Setup(g => g.ListActivePalaceLawDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<PalaceLawDefinitionSnapshot>)laws);
        return gateway;
    }

    private static AmbientPalaceLawPromulgator CreateSut(params PalaceLawDefinitionSnapshot[] laws)
        => new(CreateGateway(laws).Object, new DeterministicWeightedSelector());

    [Fact]
    public async Task PromulgateForRoomTransitionAsync_ShouldPromulgateALaw_OnTheGuaranteedFirstFloorDraw()
    {
        var run = TestGameEngineFactory.CreateRun();
        var sut = CreateSut(CreateLaw("law-a"));

        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: 1);
        await sut.PromulgateForRoomTransitionAsync(run, nextRoom);

        run.ActivePalaceLaws.Should().ContainSingle(law => law.Key == "law-a");
        run.LastPromulgationFloorIndex.Should().Be(run.FloorIndex);
    }

    [Fact]
    public async Task PromulgateForRoomTransitionAsync_ShouldNeverDrawARoomLinkedLaw()
    {
        var run = TestGameEngineFactory.CreateRun();
        var sut = CreateSut(CreateLaw("law-room-bound", roomKey: "room08"));

        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: 1);
        await sut.PromulgateForRoomTransitionAsync(run, nextRoom);

        run.ActivePalaceLaws.Should().BeEmpty(
            because: "the only candidate is room-linked (Chapitre IX) and must never enter the ambient pool.");
    }

    [Fact]
    public async Task PromulgateForRoomTransitionAsync_ShouldNotPromulgate_WhenNoCatalogLawsExist()
    {
        var run = TestGameEngineFactory.CreateRun();
        var sut = CreateSut();

        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: 1);
        await sut.PromulgateForRoomTransitionAsync(run, nextRoom);

        run.ActivePalaceLaws.Should().BeEmpty();
    }

    [Fact]
    public async Task PromulgateForRoomTransitionAsync_ShouldRetryAndConverge_WhenACandidateConflicts()
    {
        var run = TestGameEngineFactory.CreateRun();

        // Pre-activate a majeure law so any other majeure candidate is rejected by the
        // exclusivity rule, forcing the promulgator to drop it and retry against the pool.
        run.PromulgateLaw(PalaceLaw.Create(
            "law-majeure-active", "Loi majeure active", "1.0.0",
            domains: [PalaceLawDomain.Combat], isMajeure: true));

        var sut = CreateSut(
            CreateLaw("law-majeure-b", isMajeure: true),
            CreateLaw("law-ordinary"));

        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: 1);
        await sut.PromulgateForRoomTransitionAsync(run, nextRoom);

        run.ActivePalaceLaws.Should().Contain(law => law.Key == "law-ordinary");
        run.ActivePalaceLaws.Should().NotContain(law => law.Key == "law-majeure-b");
    }

    [Fact]
    public async Task PromulgateForRoomTransitionAsync_ShouldExcludeAlreadyActiveLaws()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.PromulgateLaw(PalaceLaw.Create(
            "law-a", "Loi A", "1.0.0", domains: [PalaceLawDomain.Combat]));

        var sut = CreateSut(CreateLaw("law-a"));

        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: 1);
        await sut.PromulgateForRoomTransitionAsync(run, nextRoom);

        run.ActivePalaceLaws.Should().ContainSingle(
            because: "law-a is already active — the only catalog candidate must be excluded from the pool.");
    }
}
