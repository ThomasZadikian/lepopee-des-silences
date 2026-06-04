using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.AbandonRun;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.AbandonRun;

public sealed class AbandonRunCommandHandlerTests
{
    private const string Seed = "seed-abandon-test";
    private const string GeneratorVersion = "gen-0.4.0";
    private const string MarkovMatrixVersion = "markov-0.2.0";

    [Fact]
    public async Task Handle_ShouldAbandonRun_AndPersistIt()
    {
        // Arrange
        var run = CreateRun();

        var now = new DateTimeOffset(
            2026,
            5,
            31,
            12,
            0,
            0,
            TimeSpan.Zero);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock
            .SetupGet(service => service.UtcNow)
            .Returns(now);

        var handler = new AbandonRunCommandHandler(
            repository.Object,
            clock.Object);

        // Act
        var response = await handler.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None);

        // Assert
        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.Status.Should().Be(RunStatus.Abandoned.ToString());

        run.Status.Should().Be(RunStatus.Abandoned);
        run.EndedAt.Should().Be(now);

        repository.Verify(
            repo => repo.UpdateAsync(
                It.Is<Run>(candidate =>
                    candidate.Id == run.Id &&
                    candidate.Status == RunStatus.Abandoned &&
                    candidate.EndedAt == now),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        // Arrange
        var unknownRunId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(
                new RunId(unknownRunId),
                CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var clock = new Mock<IClock>();

        var handler = new AbandonRunCommandHandler(
            repository.Object,
            clock.Object);

        // Act
        var act = async () => await handler.Handle(
            new AbandonRunCommand(unknownRunId),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        repository.Verify(
            repo => repo.UpdateAsync(
                It.IsAny<Run>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunIsAlreadyClosed()
    {
        // Arrange
        var run = CreateRun();

        run.Abandon(DateTimeOffset.UtcNow);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock
            .SetupGet(service => service.UtcNow)
            .Returns(DateTimeOffset.UtcNow.AddMinutes(1));

        var handler = new AbandonRunCommandHandler(
            repository.Object,
            clock.Object);

        // Act
        var act = async () => await handler.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("Run is already closed.");

        repository.Verify(
            repo => repo.UpdateAsync(
                It.IsAny<Run>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Run CreateRun()
    {
        return Run.StartNew(
            Guid.NewGuid(),
            Seed,
            GeneratorVersion,
            MarkovMatrixVersion,
            CreateInitialRoom(),
            DateTimeOffset.UtcNow);
    }

    private static Room CreateInitialRoom()
    {
        var roomType = RoomType.Threshold;

        var bossProfile = RoomBossProfile.Create(
            "threshold-guardian",
            "Gardien du Seuil",
            roomType,
            "High");

        var combatNode = Node.Create(
            NodeEventType.Combat,
            20,
            "combat-common",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var itemNode = Node.Create(
            NodeEventType.Item,
            15,
            "common",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var restNode = Node.Create(
            NodeEventType.Rest,
            5,
            "healing-only",
            nodeDepth: 1,
            parentNodeId: combatNode.Id,
            initialState: NodeState.Planned);

        var npcNode = Node.Create(
            new[] { NodeEvent.Create(NodeEventType.Npc, 1) },
            8,
            "narrative-choice",
            nodeDepth: 1,
            parentNodeIds: new[] { combatNode.Id, itemNode.Id },
            isRoomBossNode: false,
            initialState: NodeState.Planned);

        var rareNode = Node.Create(
            NodeEventType.Rare,
            25,
            "rare",
            nodeDepth: 1,
            parentNodeId: itemNode.Id,
            initialState: NodeState.Planned);

        var bossNode = Node.Create(
            new[] { NodeEvent.Create(NodeEventType.RoomBoss, 1) },
            riskLevel: 80,
            rewardProfile: "room-boss",
            nodeDepth: 2,
            parentNodeIds: new[] { restNode.Id, npcNode.Id, rareNode.Id },
            isRoomBossNode: true,
            initialState: NodeState.Planned);

        return Room.Create(
            0,
            roomType,
            "Threshold",
            bossProfile,
            new[]
            {
                combatNode,
                itemNode,
                restNode,
                npcNode,
                rareNode,
                bossNode
            });
    }
}