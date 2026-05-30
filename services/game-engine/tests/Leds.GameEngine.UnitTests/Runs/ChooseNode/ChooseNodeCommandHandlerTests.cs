using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.ChooseNode;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ChooseNode;

public sealed class ChooseNodeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSelectNode_AndLockOtherNodes()
    {
        var run = CreateRun();
        var selectedNode = run.CurrentRoom.Nodes.First();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ChooseNodeCommandHandler(repository.Object);

        var response = await handler.Handle(
            new ChooseNodeCommand(run.Id.Value, selectedNode.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);

        var selectedNodeDto = response.Run.CurrentRoom.Nodes
            .Single(node => node.Id == selectedNode.Id.Value);

        selectedNodeDto.State.Should().Be(NodeState.Selected.ToString());

        response.Run.CurrentRoom.Nodes
            .Where(node => node.Id != selectedNode.Id.Value)
            .Should()
            .OnlyContain(node => node.State == NodeState.Locked.ToString());

        repository.Verify(
            repo => repo.UpdateAsync(run, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var handler = new ChooseNodeCommandHandler(repository.Object);

        var act = () => handler.Handle(
            new ChooseNodeCommand(runId, nodeId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
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
            });
    }
}