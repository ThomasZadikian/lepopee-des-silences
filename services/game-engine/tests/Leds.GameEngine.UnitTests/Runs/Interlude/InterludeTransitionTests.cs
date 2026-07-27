using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Application.Interlude;
using Leds.GameEngine.Application.Interlude.EnterInterlude;
using Leds.GameEngine.Application.Interlude.GetInterlude;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Runs.EnterGridNode;
using Leds.GameEngine.Application.Runs.MoveToNextRoom;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Interlude;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.Interlude;

/// <summary>
/// PR 0.2.0 — Interlude transition flow.
///
/// Group A: EnterInterlude command handler (9 tests)
/// Group B: GetInterlude — node layout (8 tests)
/// Group C: EnterNextRoom — MoveToNextRoom from Interlude (8 tests)
/// Group D: Guards — ProgressRun / ChooseNode / ResolveCurrentEvent blocked in Interlude (3 tests)
/// </summary>
public sealed class InterludeTransitionTests
{
    // -----------------------------------------------------------------------
    // Shared helpers
    // -----------------------------------------------------------------------

    private static Run CreateRunInInterlude()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.EnterInterlude();
        return run;
    }

    private static (EnterInterludeCommandHandler handler, Mock<IRunRepository> repo, Run run)
        CreateEnterInterludeHandler(Run run)
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var provider = new DefaultInterludeNodeProvider();
        var handler = new EnterInterludeCommandHandler(repo.Object, provider);

        return (handler, repo, run);
    }

    private static (GetInterludeQueryHandler handler, Mock<IRunRepository> repo, Run run)
        CreateGetInterludeHandler(Run run)
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var provider = new DefaultInterludeNodeProvider();
        var handler = new GetInterludeQueryHandler(repo.Object, provider);

        return (handler, repo, run);
    }

    // -----------------------------------------------------------------------
    // Group A — EnterInterlude
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EnterInterlude_ShouldSucceed_WhenRunIsRoomCleared()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        var (handler, _, _) = CreateEnterInterludeHandler(run);

        var act = () => handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnterInterlude_ShouldSetRunStatusToInterlude()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        var (handler, _, _) = CreateEnterInterludeHandler(run);

        await handler.Handle(new EnterInterludeCommand(run.Id.Value), CancellationToken.None);

        run.Status.Should().Be(RunStatus.Interlude);
    }

    [Fact]
    public async Task EnterInterlude_ShouldReturnInterludeDto()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        var (handler, _, _) = CreateEnterInterludeHandler(run);

        var response = await handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        response.Interlude.Should().NotBeNull();
        response.Interlude.RunId.Should().Be(run.Id.Value);
    }

    [Fact]
    public async Task EnterInterlude_ShouldExposeDefaultInterludeNodes()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        var (handler, _, _) = CreateEnterInterludeHandler(run);

        var response = await handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        response.Interlude.Nodes.Should().HaveCount(6,
            because: "DefaultInterludeNodeProvider returns exactly 6 nodes.");
    }

    [Fact]
    public async Task EnterInterlude_ShouldFail_WhenRunIsActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Status.Should().Be(RunStatus.Active);

        var (handler, _, _) = CreateEnterInterludeHandler(run);

        var act = () => handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*RoomResolved*");
    }

    [Fact]
    public async Task EnterInterlude_ShouldFail_WhenRewardIsPending()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();

        // Manually inject a pending reward without selecting it
        var offerId = new RewardOfferId(Guid.NewGuid());
        run.SetPendingRewardOffer(offerId);

        var (handler, _, _) = CreateEnterInterludeHandler(run);

        var act = () => handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*pending reward*");
    }

    [Fact]
    public async Task EnterInterlude_ShouldFail_WhenRunIsAlreadyInterlude()
    {
        // Calling EnterInterlude a second time must fail — the run is already Interlude.
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.EnterInterlude(); // first call: succeeds

        // Re-setup the mock so the handler can retrieve the run
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var provider = new DefaultInterludeNodeProvider();
        var handler = new EnterInterludeCommandHandler(repo.Object, provider);

        var act = () => handler.Handle(
            new EnterInterludeCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task EnterInterlude_ShouldNotIncrementCurrentRoomIndex()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        var indexBefore = run.CurrentRoomIndex;

        var (handler, _, _) = CreateEnterInterludeHandler(run);

        await handler.Handle(new EnterInterludeCommand(run.Id.Value), CancellationToken.None);

        run.CurrentRoomIndex.Should().Be(indexBefore,
            because: "EnterInterlude must not advance the room index.");
    }

    [Fact]
    public async Task EnterInterlude_ShouldNotGenerateNextRoom()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        var roomCountBefore = run.Rooms.Count;

        var (handler, _, _) = CreateEnterInterludeHandler(run);

        await handler.Handle(new EnterInterludeCommand(run.Id.Value), CancellationToken.None);

        run.Rooms.Count.Should().Be(roomCountBefore,
            because: "EnterInterlude must not generate a new room.");
    }

    // -----------------------------------------------------------------------
    // Group B — GetInterlude node layout
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetInterlude_ShouldReturnPlayerNode()
    {
        var run = CreateRunInInterlude();
        var (handler, _, _) = CreateGetInterludeHandler(run);

        var response = await handler.Handle(new GetInterludeQuery(run.Id.Value), CancellationToken.None);

        response.Interlude.Nodes.Should().ContainSingle(n =>
            n.Type == InterludeNodeType.Player.ToString() &&
            n.PositionSlot == "center" &&
            n.IsEnabled);
    }

    [Fact]
    public async Task GetInterlude_ShouldReturnEliseNode()
    {
        var run = CreateRunInInterlude();
        var (handler, _, _) = CreateGetInterludeHandler(run);

        var response = await handler.Handle(new GetInterludeQuery(run.Id.Value), CancellationToken.None);

        response.Interlude.Nodes.Should().ContainSingle(n =>
            n.Type == InterludeNodeType.Elise.ToString() &&
            n.PositionSlot == "left" &&
            n.IsEnabled);
    }

    [Fact]
    public async Task GetInterlude_ShouldReturnInventoryNode()
    {
        var run = CreateRunInInterlude();
        var (handler, _, _) = CreateGetInterludeHandler(run);

        var response = await handler.Handle(new GetInterludeQuery(run.Id.Value), CancellationToken.None);

        response.Interlude.Nodes.Should().ContainSingle(n =>
            n.Type == InterludeNodeType.Inventory.ToString() &&
            n.PositionSlot == "right" &&
            n.IsEnabled);
    }

    [Fact]
    public async Task GetInterlude_ShouldReturnJournalNode()
    {
        var run = CreateRunInInterlude();
        var (handler, _, _) = CreateGetInterludeHandler(run);

        var response = await handler.Handle(new GetInterludeQuery(run.Id.Value), CancellationToken.None);

        response.Interlude.Nodes.Should().ContainSingle(n =>
            n.Type == InterludeNodeType.Journal.ToString() &&
            n.PositionSlot == "top" &&
            n.IsEnabled);
    }

    [Fact]
    public async Task GetInterlude_ShouldReturnPlaceholderNodes()
    {
        var run = CreateRunInInterlude();
        var (handler, _, _) = CreateGetInterludeHandler(run);

        var response = await handler.Handle(new GetInterludeQuery(run.Id.Value), CancellationToken.None);

        var placeholders = response.Interlude.Nodes
            .Where(n => n.Type == InterludeNodeType.Placeholder.ToString())
            .ToList();

        placeholders.Should().HaveCount(2);
        placeholders.Should().AllSatisfy(n => n.IsEnabled.Should().BeFalse());
        placeholders.Select(n => n.PositionSlot).Should()
            .BeEquivalentTo(["bottom-left", "bottom-right"]);
    }

    [Fact]
    public async Task GetInterlude_ShouldNotExposeMapNodeRiskOrRewardFields()
    {
        var run = CreateRunInInterlude();
        var (handler, _, _) = CreateGetInterludeHandler(run);

        var response = await handler.Handle(new GetInterludeQuery(run.Id.Value), CancellationToken.None);

        // InterludeNodeDto only has: Id, Type, Label, Description, PositionSlot, IsEnabled, ActionKey
        // Verify none of the expected MapNode-only fields are present on the DTO
        var nodeDto = response.Interlude.Nodes.First();
        var dtoType = nodeDto.GetType();

        dtoType.GetProperty("RiskLevel").Should().BeNull(
            because: "InterludeNodeDto must not expose RiskLevel.");
        dtoType.GetProperty("RewardProfile").Should().BeNull(
            because: "InterludeNodeDto must not expose RewardProfile.");
        dtoType.GetProperty("Row").Should().BeNull(
            because: "InterludeNodeDto must not expose Row.");
        dtoType.GetProperty("Lane").Should().BeNull(
            because: "InterludeNodeDto must not expose Lane.");
    }

    [Fact]
    public async Task GetInterlude_ShouldExposeCurrentRoomIndex()
    {
        var run = CreateRunInInterlude();
        var (handler, _, _) = CreateGetInterludeHandler(run);

        var response = await handler.Handle(new GetInterludeQuery(run.Id.Value), CancellationToken.None);

        response.Interlude.CurrentRoomIndex.Should().Be(run.CurrentRoomIndex);
    }

    [Fact]
    public async Task GetInterlude_ShouldExposeDisplayRoomNumber()
    {
        var run = CreateRunInInterlude();
        var (handler, _, _) = CreateGetInterludeHandler(run);

        var response = await handler.Handle(new GetInterludeQuery(run.Id.Value), CancellationToken.None);

        response.Interlude.DisplayRoomNumber.Should().Be(run.CurrentRoomIndex + 1);
    }

    // -----------------------------------------------------------------------
    // Group C — EnterNextRoom (MoveToNextRoom from Interlude)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EnterNextRoom_ShouldFail_WhenRunIsNotInInterlude()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        // Status is RoomResolved, NOT Interlude

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        var handler = new MoveToNextRoomCommandHandler(repo.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);

        var act = () => handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*Interlude*");
    }

    [Fact]
    public async Task EnterNextRoom_ShouldIncrementCurrentRoomIndex()
    {
        var run = CreateRunInInterlude();
        var indexBefore = run.CurrentRoomIndex;
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(g => g.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        var handler = new MoveToNextRoomCommandHandler(repo.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);
        var response = await handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.CurrentRoomIndex.Should().Be(indexBefore + 1);
    }

    [Fact]
    public async Task EnterNextRoom_ShouldGenerateNewCurrentRoom()
    {
        var run = CreateRunInInterlude();
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(g => g.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        var handler = new MoveToNextRoomCommandHandler(repo.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);
        var response = await handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.CurrentRoom.Id.Should().Be(nextRoom.Id.Value);
    }

    [Fact]
    public async Task EnterNextRoom_ShouldSetRunStatusToActive()
    {
        var run = CreateRunInInterlude();
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(g => g.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        var handler = new MoveToNextRoomCommandHandler(repo.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);
        var response = await handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Status.Should().Be(RunStatus.Active.ToString());
    }

    [Fact]
    public async Task EnterNextRoom_ShouldPreservePreviousRoomInRunHistory()
    {
        var run = CreateRunInInterlude();
        var previousRoomId = run.CurrentRoom.Id;
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(g => g.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        var handler = new MoveToNextRoomCommandHandler(repo.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);
        await handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        run.Rooms.Should().Contain(r => r.Id == previousRoomId,
            because: "Previous room must remain in the run history after MoveToNextRoom.");
    }

    [Fact]
    public async Task EnterNextRoom_ShouldGenerateNonThresholdRoom_WhenCurrentRoomIndexIsGreaterThanZero()
    {
        var run = CreateRunInInterlude();
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(g => g.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        var handler = new MoveToNextRoomCommandHandler(repo.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);

        // Verify generator was called with the run (depth-aware call)
        await handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        generator.Verify(g => g.GenerateNextRoomAsync(run, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task EnterNextRoom_ShouldReturnRunDtoWithUpdatedCurrentRoomIndex()
    {
        var run = CreateRunInInterlude();
        var expectedIndex = run.CurrentRoomIndex + 1;
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(g => g.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        var handler = new MoveToNextRoomCommandHandler(repo.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);
        var response = await handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.CurrentRoomIndex.Should().Be(expectedIndex);
    }

    [Fact]
    public async Task EnterNextRoom_ShouldResetRoomProgressionForNewRoom()
    {
        var run = CreateRunInInterlude();
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(g => g.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        var handler = new MoveToNextRoomCommandHandler(repo.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);
        await handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        // New room starts at row 0, with available nodes
        run.CurrentRoom.CurrentNodeDepth.Should().Be(0,
            because: "New room must start at depth 0.");
        run.CurrentRoom.AvailableNodes.Should().NotBeEmpty(
            because: "New room must have available nodes to begin.");
    }

    // -----------------------------------------------------------------------
    // Group D — Guards: Interlude blocks Run progression commands
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldFail_WhenRunIsInterlude()
    {
        var run = CreateRunInInterlude();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var choiceResolver = new Mock<ICurrentEventChoiceRequirementResolver>();
        var handler = new ProgressRunCommandHandler(repo.Object, choiceResolver.Object);

        var act = () => handler.Handle(
            new ProgressRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*Interlude*");
    }

    [Fact]
    public async Task EnterGridNode_ShouldFail_WhenRunIsInterlude()
    {
        var run = CreateRunInInterlude();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new EnterGridNodeCommandHandler(repo.Object);
        var anyNodeId = Guid.NewGuid();

        var act = () => handler.Handle(
            new EnterGridNodeCommand(run.Id.Value, anyNodeId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*Interlude*");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldFail_WhenRunIsInterlude()
    {
        var run = CreateRunInInterlude();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        // ResolveCurrentEventCommandHandler requires additional dependencies;
        // the Interlude guard fires before any of them are reached.
        var nodeEventResolverDispatcher = new Mock<INodeEventResolverDispatcher>();
        var eventContentResolver = new Mock<IEventContentResolver>();
        var catalogGateway = new Mock<ICatalogContentGateway>();
        var combatFactory = new Mock<ICombatInstanceFactory>();

        var handler = new ResolveCurrentEventCommandHandler(
            repo.Object,
            nodeEventResolverDispatcher.Object,
            eventContentResolver.Object,
            catalogGateway.Object,
            combatFactory.Object,
            new Mock<ICombatEncounterDraftGenerator>().Object,
            new Mock<ICombatFactory>().Object,
            Mock.Of<Leds.GameEngine.Application.Combats.Tactical.ITacticalCombatFactory>(),
            new Mock<IRewardOfferRepository>().Object,
            new Leds.GameEngine.Application.Rewards.RewardOfferFactory.RewardOfferFactory(
                new Mock<Leds.GameEngine.Application.Combats.ICombatRiskProfileResolver>().Object,
                Mock.Of<ICatalogContentGateway>(),
                new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>())),
            Mock.Of<IEnemyCombatTurnResolver>(),
             Mock.Of<ICombatResolutionService>(),
            Mock.Of<Leds.GameEngine.Application.Combats.Atb.IAtbCombatPreparer>(),
            Mock.Of<IClock>());

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*Interlude*");
    }
}
