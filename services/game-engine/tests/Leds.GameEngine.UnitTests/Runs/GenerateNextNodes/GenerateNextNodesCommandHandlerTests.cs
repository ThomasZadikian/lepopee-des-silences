using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.GenerateNextNodes;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GenerateNextNodes;

public sealed class GenerateNextNodesCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldAddNextNodesToCurrentRoom()
    {
        var run = CreateRunWithResolvedCurrentEvent();
        var resolvedNode = run.CurrentRoom.GetResolvedNodeAtCurrentDepth();

        var nextNodes = new[]
        {
            Node.Create(
                NodeEventType.Memory,
                10,
                "narrative",
                nodeDepth: 1,
                parentNodeId: resolvedNode.Id)
        };

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator
            .Setup(service => service.GenerateNextNodes(run))
            .Returns(nextNodes);

        var handler = new GenerateNextNodesCommandHandler(
            repository.Object,
            generator.Object);

        var response = await handler.Handle(
            new GenerateNextNodesCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.CurrentRoom.State.Should().Be(RoomState.Active.ToString());
        response.Run.CurrentRoom.CurrentNodeDepth.Should().Be(1);
        response.Run.CurrentRoom.AvailableNodes.Should().ContainSingle();
        response.Run.CurrentRoom.AvailableNodes.Single().ParentNodeId.Should().Be(resolvedNode.Id.Value);

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

        var generator = new Mock<IRunGenerator>();

        var handler = new GenerateNextNodesCommandHandler(
            repository.Object,
            generator.Object);

        var act = () => handler.Handle(
            new GenerateNextNodesCommand(runId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenCurrentEventIsNotResolved()
    {
        var run = CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator
            .Setup(service => service.GenerateNextNodes(run))
            .Returns(Array.Empty<Node>());

        var handler = new GenerateNextNodesCommandHandler(
            repository.Object,
            generator.Object);

        var act = () => handler.Handle(
            new GenerateNextNodesCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("Current node event must be resolved before adding next nodes.");
    }

    private static Run CreateRunWithResolvedCurrentEvent()
    {
        var run = CreateRun();

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);
        run.ResolveCurrentEvent();

        return run;
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