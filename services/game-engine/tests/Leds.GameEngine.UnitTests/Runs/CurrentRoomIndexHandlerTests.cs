using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Application.PalaceLaws.Ports;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Rewards.SelectReward;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Application.Runs.EnterGridNode;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.PalaceIndicators;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// PR 0.1.10 — Group B: verifies that every handler response exposes
/// <c>CurrentRoomIndex = 0</c> and <c>CurrentRoomNumber = 1</c> via <see cref="RunDto"/>.
///
/// These tests confirm that <see cref="RunDto.FromDomain"/> correctly maps
/// <see cref="Run.CurrentRoomIndex"/> across all run-lifecycle endpoints.
/// </summary>
public sealed class CurrentRoomIndexHandlerTests
{
    // -----------------------------------------------------------------------
    // GetRunById
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetRunById_ShouldExposeCurrentRoomIndexInRunDto()
    {
        var run = TestGameEngineFactory.CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var palaceIndicatorRepository = new Mock<IPalaceIndicatorRepository>();
        palaceIndicatorRepository
            .Setup(r => r.GetByRunIdAsync(run.Id.Value, CancellationToken.None))
            .ReturnsAsync([]);

        var handler = new GetRunByIdQueryHandler(
            repository.Object,
            palaceIndicatorRepository.Object,
            new PalacePublicIndicatorProjectionService());

        var response = await handler.Handle(
            new GetRunByIdQuery(run.Id.Value),
            CancellationToken.None);

        response.Run.CurrentRoomIndex.Should().Be(0,
            because: "Threshold is always the first room — CurrentRoomIndex must be 0.");
        response.Run.CurrentRoomNumber.Should().Be(1,
            because: "CurrentRoomNumber is one-based: first room displays as Salle 1.");
    }

    // -----------------------------------------------------------------------
    // EnterGridNode
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EnterGridNode_ShouldPreserveCurrentRoomIndex()
    {
        var run = TestGameEngineFactory.CreateRun();
        var selectedNode = run.CurrentRoom.AvailableNodes.First();
        run.MoveParty(selectedNode.Lane, selectedNode.Row);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new EnterGridNodeCommandHandler(repository.Object);

        var response = await handler.Handle(
            new EnterGridNodeCommand(run.Id.Value, selectedNode.Id.Value),
            CancellationToken.None);

        response.Run.CurrentRoomIndex.Should().Be(0,
            because: "Entering a node within the current room must not change CurrentRoomIndex.");
    }

    // -----------------------------------------------------------------------
    // ResolveCurrentEvent — tested via domain + DTO projection
    // (handler has too many infrastructure dependencies to mock inline)
    // -----------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldPreserveCurrentRoomIndex()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(
            NodeEventType.Combat);

        runWithNode.Run.ResolveCurrentEvent();

        var dto = RunDto.FromDomain(runWithNode.Run);

        dto.CurrentRoomIndex.Should().Be(0,
            because: "Resolving a node event is intra-room progression — CurrentRoomIndex must stay 0.");
        dto.CurrentRoomNumber.Should().Be(1);
    }

    // -----------------------------------------------------------------------
    // ProgressRun
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldPreserveCurrentRoomIndex()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(
            NodeEventType.Combat);

        var run = runWithNode.Run;

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var choiceResolver = new Mock<ICurrentEventChoiceRequirementResolver>();
        choiceResolver
            .Setup(r => r.RequiresChoice(It.IsAny<MapNode>()))
            .Returns(false);

        var handler = new ProgressRunCommandHandler(repository.Object, choiceResolver.Object);

        var response = await handler.Handle(
            new ProgressRunCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.CurrentRoomIndex.Should().Be(0,
            because: "Advancing to the next node layer within a room must not change CurrentRoomIndex.");
    }

    // -----------------------------------------------------------------------
    // SelectReward
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SelectReward_ShouldPreserveCurrentRoomIndex()
    {
        var run = TestGameEngineFactory.CreateRun();

        var factory = new RewardOfferFactory(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));
        var offer = factory.CreateCombatRewardOffer(
            RewardSource.Combat,
            NodeEventType.Combat,
            riskLevel: (int)RiskTier.Tendu);

        run.SetPendingRewardOffer(offer.Id);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(
            runRepository.Object,
            rewardRepository.Object,
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<IPlayerProfileGateway>());

        var choiceId = offer.Choices.First().Id;

        var response = await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        response.Run.CurrentRoomIndex.Should().Be(0,
            because: "Selecting a reward is a post-combat operation within the same room — CurrentRoomIndex must not change.");
    }
}
