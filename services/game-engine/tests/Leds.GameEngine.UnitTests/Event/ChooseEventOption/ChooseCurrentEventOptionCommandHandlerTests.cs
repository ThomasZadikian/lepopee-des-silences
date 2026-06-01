using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Events.ChooseEventOption;

public sealed class ChooseCurrentEventOptionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldResolveChoice_WhenCurrentEventIsResolved()
    {
        var runWithNode = CreateRunWithResolvedCurrentEvent(NodeEventType.Npc);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(runWithNode.Run.Id, CancellationToken.None))
            .ReturnsAsync(runWithNode.Run);

        var dispatcher = new Mock<ICurrentEventChoiceResolverDispatcher>();
        dispatcher
            .Setup(service => service.Resolve(It.IsAny<CurrentEventChoiceResolutionContext>()))
            .Returns(CurrentEventChoiceResolutionResult.Create(
                "listen",
                accepted: true,
                "Choice resolved.",
                new[]
                {
                    new NarrativeFragmentDto("Elise", "Test fragment.")
                }));

        var handler = new ChooseCurrentEventOptionCommandHandler(
            repository.Object,
            dispatcher.Object);

        var response = await handler.Handle(
            new ChooseCurrentEventOptionCommand(runWithNode.Run.Id.Value, "listen"),
            CancellationToken.None);

        response.Run.Id.Should().Be(runWithNode.Run.Id.Value);
        response.Result.ChoiceId.Should().Be("listen");
        response.Result.Accepted.Should().BeTrue();
        response.Result.Message.Should().Be("Choice resolved.");
        response.Result.NarrativeFragments.Should().ContainSingle();

        dispatcher.Verify(
            service => service.Resolve(
                It.Is<CurrentEventChoiceResolutionContext>(context =>
                    context.Run == runWithNode.Run &&
                    context.Node.Id == runWithNode.TargetNode.Id &&
                    context.ChoiceId == "listen")),
            Times.Once);

        repository.Verify(
            repo => repo.UpdateAsync(runWithNode.Run, CancellationToken.None),
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

        var dispatcher = new Mock<ICurrentEventChoiceResolverDispatcher>();

        var handler = new ChooseCurrentEventOptionCommandHandler(
            repository.Object,
            dispatcher.Object);

        var act = async () => await handler.Handle(
            new ChooseCurrentEventOptionCommand(runId, "listen"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();

        dispatcher.Verify(
            service => service.Resolve(It.IsAny<CurrentEventChoiceResolutionContext>()),
            Times.Never);

        repository.Verify(
            repo => repo.UpdateAsync(It.IsAny<Run>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenCurrentEventIsNotResolved()
    {
        var runWithNode = CreateRunWithActiveCurrentEvent(NodeEventType.Npc);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(runWithNode.Run.Id, CancellationToken.None))
            .ReturnsAsync(runWithNode.Run);

        var dispatcher = new Mock<ICurrentEventChoiceResolverDispatcher>();

        var handler = new ChooseCurrentEventOptionCommandHandler(
            repository.Object,
            dispatcher.Object);

        var act = async () => await handler.Handle(
            new ChooseCurrentEventOptionCommand(runWithNode.Run.Id.Value, "listen"),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Current event must be resolved before choosing an event option.");

        dispatcher.Verify(
            service => service.Resolve(It.IsAny<CurrentEventChoiceResolutionContext>()),
            Times.Never);
    }

    private static RunWithTargetNode CreateRunWithResolvedCurrentEvent(
        NodeEventType eventType)
    {
        var runWithNode = CreateRun(eventType);

        runWithNode.Run.ChooseNode(runWithNode.TargetNode.Id);
        runWithNode.Run.ResolveCurrentEvent();

        return runWithNode;
    }

    private static RunWithTargetNode CreateRunWithActiveCurrentEvent(
        NodeEventType eventType)
    {
        return CreateRun(eventType);
    }

    private static RunWithTargetNode CreateRun(NodeEventType eventType)
    {
        var roomWithNode = CreateRoom(eventType);

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-current-event-choice-handler-test",
            "gen-0.2.0",
            "markov-0.2.0",
            roomWithNode.Room,
            DateTimeOffset.UtcNow);

        return new RunWithTargetNode(run, roomWithNode.TargetNode);
    }

    private static RoomWithTargetNode CreateRoom(NodeEventType eventType)
    {
        var roomType = RoomType.Threshold;

        var bossProfile = RoomBossProfile.Create(
            "threshold-guardian",
            "Gardien du Seuil",
            roomType,
            "High");

        var targetNode = Node.Create(
            eventType,
            riskLevel: 20,
            rewardProfile: "test-reward",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var alternativeStartNode = Node.Create(
            NodeEventType.Item,
            riskLevel: 10,
            rewardProfile: "common",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var middleNode = Node.Create(
            NodeEventType.Combat,
            riskLevel: 30,
            rewardProfile: "combat-common",
            nodeDepth: 1,
            parentNodeId: targetNode.Id,
            initialState: NodeState.Planned);

        var branchNodeA = Node.Create(
            NodeEventType.Rest,
            riskLevel: 5,
            rewardProfile: "healing-only",
            nodeDepth: 1,
            parentNodeId: alternativeStartNode.Id,
            initialState: NodeState.Planned);

        var branchNodeB = Node.Create(
            NodeEventType.Rare,
            riskLevel: 40,
            rewardProfile: "rare",
            nodeDepth: 1,
            parentNodeId: alternativeStartNode.Id,
            initialState: NodeState.Planned);

        var bossNode = Node.Create(
            new[] { NodeEvent.Create(NodeEventType.RoomBoss, 1) },
            riskLevel: 80,
            rewardProfile: "room-boss",
            nodeDepth: 2,
            parentNodeIds: new[] { middleNode.Id, branchNodeA.Id, branchNodeB.Id },
            isRoomBossNode: true,
            initialState: NodeState.Planned);

        var room = Room.Create(
            0,
            roomType,
            "Threshold",
            bossProfile,
            new[]
            {
                targetNode,
                alternativeStartNode,
                middleNode,
                branchNodeA,
                branchNodeB,
                bossNode
            });

        return new RoomWithTargetNode(room, targetNode);
    }

    private sealed record RunWithTargetNode(
        Run Run,
        Node TargetNode);

    private sealed record RoomWithTargetNode(
        Room Room,
        Node TargetNode);
}