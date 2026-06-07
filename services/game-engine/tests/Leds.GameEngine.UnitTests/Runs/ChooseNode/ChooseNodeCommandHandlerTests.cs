using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.ChooseNode;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ChooseNode;

public sealed class ChooseNodeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSelectNode_LockSiblings_AndMarkUnreachableBranches()
    {
        var run = TestGameEngineFactory.CreateRun();
        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = new ChooseNodeCommandHandler(repository.Object);

        var response = await handler.Handle(
            new ChooseNodeCommand(run.Id.Value, selectedNode.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.CurrentRoom.State.Should().Be(RoomState.NodeSelected.ToString());
        response.Run.CurrentRoom.CurrentNodeDepth.Should().Be(0);

        var allNodes = response.Run.CurrentRoom.Nodes.ToArray();

        allNodes.Should().HaveCount(response.Run.CurrentRoom.TotalNodeCount);
        allNodes.Should().HaveCountGreaterThanOrEqualTo(6);
        allNodes.Should().HaveCountLessThanOrEqualTo(10);

        var selectedNodeDto = allNodes
            .Single(node => node.Id == selectedNode.Id.Value);

        selectedNodeDto.State.Should().Be(NodeState.Selected.ToString());
        selectedNodeDto.Row.Should().Be(0);

        allNodes
            .Where(node => node.Row == 0 && node.Id != selectedNode.Id.Value)
            .Should()
            .OnlyContain(node => node.State == NodeState.Locked.ToString());

        allNodes
            .Where(node => node.Row > 0)
            .Should()
            .OnlyContain(node =>
                node.State == NodeState.Planned.ToString() ||
                node.State == NodeState.Unreachable.ToString());

        allNodes
            .Where(node => node.Row > 0 && IsReachableFrom(selectedNode.Id.Value, node, allNodes))
            .Should()
            .OnlyContain(node => node.State == NodeState.Planned.ToString());

        allNodes
            .Where(node => node.Row > 0 && !IsReachableFrom(selectedNode.Id.Value, node, allNodes))
            .Should()
            .OnlyContain(node => node.State == NodeState.Unreachable.ToString());

        response.Run.CurrentRoom.AvailableNodes.Should().BeEmpty();

        allNodes
            .Should()
            .ContainSingle(node => node.IsBoss);

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

    private static bool IsReachableFrom(
        Guid ancestorNodeId,
        Leds.GameEngine.Application.Runs.Dtos.MapNodeDto node,
        IReadOnlyCollection<Leds.GameEngine.Application.Runs.Dtos.MapNodeDto> allNodes)
    {
        if (node.ParentNodeIds.Contains(ancestorNodeId))
        {
            return true;
        }

        foreach (var parentNodeId in node.ParentNodeIds)
        {
            var parent = allNodes.SingleOrDefault(candidate => candidate.Id == parentNodeId);

            if (parent is not null && IsReachableFrom(ancestorNodeId, parent, allNodes))
            {
                return true;
            }
        }

        return false;
    }
}