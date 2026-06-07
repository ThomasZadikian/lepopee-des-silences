using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ResolveCurrentEvent;

public sealed class ResolveCurrentEventCommandHandlerTests
{
    private static Mock<INodeEventResolverDispatcher> CreateDispatcherMock(
        NodeEventResolutionKind resolutionKind = NodeEventResolutionKind.NarrativeFragmentRevealed)
    {
        var dispatcher = new Mock<INodeEventResolverDispatcher>();

        dispatcher
            .Setup(service => service.Resolve(It.IsAny<NodeEventResolutionContext>()))
            .Returns(NodeEventResolutionResult.Create(
                resolutionKind,
                "Test outcome",
                "Test outcome description",
                narrativeFragments: new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Test narrative fragment.")
                }));

        return dispatcher;
    }

    private static Mock<IEventContentResolver> CreateContentResolverMock()
    {
        return new Mock<IEventContentResolver>();
    }

    private static Mock<ICatalogContentGateway> CreateCatalogGatewayMock()
    {
        return new Mock<ICatalogContentGateway>();
    }

    private static Mock<ICombatInstanceFactory> CreateCombatFactoryMock()
    {
        return new Mock<ICombatInstanceFactory>();
    }

    private static Mock<ICombatInstanceRepository> CreateCombatRepositoryMock()
    {
        return new Mock<ICombatInstanceRepository>();
    }

    [Fact]
    public async Task Handle_ShouldResolveCurrentEvent_AndKeepRunActive_WhenRoomBossIsNotResolved()
    {
        var run = TestGameEngineFactory.CreateRun();
        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var dispatcher = CreateDispatcherMock();

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            dispatcher.Object,
            CreateContentResolverMock().Object,
            CreateCatalogGatewayMock().Object,
            CreateCombatFactoryMock().Object,
            CreateCombatRepositoryMock().Object);

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        response.Run.CurrentRoom.State.Should().Be(RoomState.NodeResolved.ToString());

        var resolvedNode = response.Run.CurrentRoom.Nodes
            .Single(node => node.Id == selectedNode.Id.Value);

        resolvedNode.State.Should().Be(NodeState.Resolved.ToString());

        repository.Verify(
            repo => repo.UpdateAsync(run, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var dispatcher = CreateDispatcherMock();

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            dispatcher.Object,
            CreateContentResolverMock().Object,
            CreateCatalogGatewayMock().Object,
            CreateCombatFactoryMock().Object,
            CreateCombatRepositoryMock().Object);

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(runId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenNoNodeWasSelected()
    {
        var run = TestGameEngineFactory.CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var dispatcher = CreateDispatcherMock();

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            dispatcher.Object,
            CreateContentResolverMock().Object,
            CreateCatalogGatewayMock().Object,
            CreateCombatFactoryMock().Object,
            CreateCombatRepositoryMock().Object);

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("No node has been selected for the current room depth.");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldFail_WhenSelectedNodeAlreadyResolved()
    {
        // The node is already Resolved — no Selected node exists at current depth.
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Item);
        var run = runWithNode.Run;

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            CreateDispatcherMock().Object,
            CreateContentResolverMock().Object,
            CreateCatalogGatewayMock().Object,
            CreateCombatFactoryMock().Object,
            CreateCombatRepositoryMock().Object);

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("No node has been selected for the current room depth.");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldNotModifyRoomMapTopology()
    {
        var run = TestGameEngineFactory.CreateRun();
        var nodeCountBefore = run.CurrentRoom.Nodes.Count;
        var selectedNode = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            CreateDispatcherMock().Object,
            CreateContentResolverMock().Object,
            CreateCatalogGatewayMock().Object,
            CreateCombatFactoryMock().Object,
            CreateCombatRepositoryMock().Object);

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.CurrentRoom.Nodes.Should().HaveCount(nodeCountBefore,
            because: "ResolveCurrentEvent must not add or remove nodes from the RoomMap.");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldKeepRunProgressionConsistent()
    {
        var run = TestGameEngineFactory.CreateRun();
        var selectedNode = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            CreateDispatcherMock().Object,
            CreateContentResolverMock().Object,
            CreateCatalogGatewayMock().Object,
            CreateCombatFactoryMock().Object,
            CreateCombatRepositoryMock().Object);

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        // Run remains Active (boss not yet reached).
        response.Run.Status.Should().Be(RunStatus.Active.ToString());

        // Room transitioned to NodeResolved — ready for progress.
        response.Run.CurrentRoom.State.Should().Be(RoomState.NodeResolved.ToString());

        // CurrentNodeDepth has not advanced (ProgressRun is a separate command).
        response.Run.CurrentRoom.CurrentNodeDepth.Should().Be(0);

        // The target node is now Resolved.
        response.Run.CurrentRoom.Nodes
            .Single(n => n.Id == selectedNode.Id.Value)
            .State.Should().Be(NodeState.Resolved.ToString());

        // Repository was updated exactly once.
        repository.Verify(
            repo => repo.UpdateAsync(run, CancellationToken.None),
            Times.Once);

        // Outcome carries the correct node metadata.
        response.Outcome.NodeId.Should().Be(selectedNode.Id.Value);
        response.Outcome.Title.Should().NotBeNullOrWhiteSpace();
        response.Outcome.Description.Should().NotBeNullOrWhiteSpace();
    }
}