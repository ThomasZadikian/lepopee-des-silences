using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ResolveCurrentEvent;

public sealed class ResolveCurrentEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldResolveCurrentEvent_AndKeepRunActive_WhenRoomBossIsNotResolved()
    {
        var run = CreateRun();
        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ResolveCurrentEventCommandHandler(repository.Object);

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

        var handler = new ResolveCurrentEventCommandHandler(repository.Object);

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
        var run = CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ResolveCurrentEventCommandHandler(repository.Object);

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("Room must have a selected node before resolving an event.");
    }

    private static Run CreateRun()
    {
        return Run.StartNew(
            Guid.NewGuid(),
            "seed-test-001",
            "gen-0.1.0",
            "markov-0.1.0",
            CreateInitialRoom(),
            DateTimeOffset.UtcNow);
    }

    private static Room CreateInitialRoom()
    {
        return Room.Create(
            0,
            "Threshold",
            new[]
            {
                Node.Create(NodeEventType.Combat, 20, "common"),
                Node.Create(NodeEventType.Memory, 10, "common"),
                Node.Create(NodeEventType.Rest, 5, "none"),
                Node.Create(NodeEventType.Item, 15, "common")
            },
            maxNodeDepth: 3);
    }
}