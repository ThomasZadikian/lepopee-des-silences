using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.ExitMidRoom;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ExitMidRoom;

public sealed class ExitMidRoomCommandHandlerTests
{
    private static Run CreateActiveRunWithSomeProgression()
    {
        var run = TestGameEngineFactory.CreateRun();
        var node = run.CurrentRoom.AvailableNodes.First();
        TestGameEngineFactory.EnterNode(run, node);
        run.ResolveCurrentEvent();
        run.ProgressCurrentRoom();
        return run;
    }

    private static Mock<IPlayerProfileGateway> CreatePlayerProfileGateway()
    {
        var gateway = new Mock<IPlayerProfileGateway>();
        gateway
            .Setup(g => g.UpsertNpcReputationScoresAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<NpcReputationScoreView>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return gateway;
    }

    private static (ExitMidRoomCommandHandler handler, Mock<IRunRepository> repo, Mock<IClock> clock)
        CreateHandler(Run run)
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var handler = new ExitMidRoomCommandHandler(
            repo.Object, CreatePlayerProfileGateway().Object, clock.Object, Mock.Of<ILogger<ExitMidRoomCommandHandler>>());
        return (handler, repo, clock);
    }

    [Fact]
    public async Task Handle_ShouldExitMidRoom_AndPersistRun()
    {
        var run = CreateActiveRunWithSomeProgression();
        var now = new DateTimeOffset(2026, 6, 9, 14, 0, 0, TimeSpan.Zero);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(now);

        var handler = new ExitMidRoomCommandHandler(
            repo.Object, CreatePlayerProfileGateway().Object, clock.Object, Mock.Of<ILogger<ExitMidRoomCommandHandler>>());
        var response = await handler.Handle(
            new ExitMidRoomCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Status.Should().Be(RunStatus.Suspended.ToString());
        response.Run.CanResume.Should().BeTrue();
        response.Run.SavedAt.Should().Be(now);

        repo.Verify(
            r => r.UpdateAsync(
                It.Is<Run>(candidate =>
                    candidate.Id == run.Id &&
                    candidate.Status == RunStatus.Suspended),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var handler = new ExitMidRoomCommandHandler(
            repo.Object, CreatePlayerProfileGateway().Object, clock.Object, Mock.Of<ILogger<ExitMidRoomCommandHandler>>());

        var act = () => handler.Handle(
            new ExitMidRoomCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("*Run*");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunHasActiveCombat()
    {
        var run = CreateActiveRunWithSomeProgression();
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = TestTacticalCombatHelper.Create(run.Id, RoomId.New(), NodeId.New(), [ally], [enemy]);
        run.StartTacticalCombat(combat);

        var (handler, _, _) = CreateHandler(run);

        var act = () => handler.Handle(
            new ExitMidRoomCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*active combat*");
    }

    [Fact]
    public async Task Handle_ShouldSucceed_AfterBossNodeIsResolved()
    {
        // The boss no longer locks the run out of Active — ExitMidRoom stays available.
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        var (handler, _, _) = CreateHandler(run);

        var response = await handler.Handle(
            new ExitMidRoomCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Status.Should().Be(RunStatus.Suspended.ToString());
    }

    [Fact]
    public async Task Handle_ShouldSyncNpcReputation_WhenRunHasRelationships()
    {
        var run = CreateActiveRunWithSomeProgression();
        run.AdjustNpcRelationshipScore("npc.thomas", 5);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var playerProfileGateway = CreatePlayerProfileGateway();

        var handler = new ExitMidRoomCommandHandler(
            repo.Object, playerProfileGateway.Object, clock.Object, Mock.Of<ILogger<ExitMidRoomCommandHandler>>());

        await handler.Handle(
            new ExitMidRoomCommand(run.Id.Value),
            CancellationToken.None);

        playerProfileGateway.Verify(
            g => g.UpsertNpcReputationScoresAsync(
                run.PlayerId,
                run.Id.Value,
                It.Is<IReadOnlyCollection<NpcReputationScoreView>>(scores =>
                    scores.Count == 1 && scores.Single().NpcKey == "npc.thomas" && scores.Single().Score == 5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}