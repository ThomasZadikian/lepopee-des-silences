using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.SaveAndExitRun;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.SaveAndExitRun;

public sealed class SaveAndExitRunCommandHandlerTests
{
    private static Mock<IPlayerProfileGateway> CreatePlayerProfileGateway()
    {
        var gateway = new Mock<IPlayerProfileGateway>();
        gateway
            .Setup(g => g.UpsertNpcReputationScoresAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<NpcReputationScoreView>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return gateway;
    }

    [Fact]
    public async Task Handle_ShouldSuspendRun_AndPersistIt_WhenAtSafePoint()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();

        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(now);

        var handler = new SaveAndExitRunCommandHandler(
            repository.Object, CreatePlayerProfileGateway().Object, clock.Object, Mock.Of<ILogger<SaveAndExitRunCommandHandler>>());

        var response = await handler.Handle(
            new SaveAndExitRunCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.Status.Should().Be(RunStatus.Suspended.ToString());

        run.Status.Should().Be(RunStatus.Suspended);

        repository.Verify(
            repo => repo.UpdateAsync(
                It.Is<Run>(candidate =>
                    candidate.Id == run.Id &&
                    candidate.Status == RunStatus.Suspended),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSuspendRun_WhenInInterlude()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.EnterInterlude();
        run.Status.Should().Be(RunStatus.Interlude);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var handler = new SaveAndExitRunCommandHandler(
            repository.Object, CreatePlayerProfileGateway().Object, clock.Object, Mock.Of<ILogger<SaveAndExitRunCommandHandler>>());

        var response = await handler.Handle(
            new SaveAndExitRunCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Status.Should().Be(RunStatus.Suspended.ToString());
        run.Status.Should().Be(RunStatus.Suspended);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(new RunId(unknownRunId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var clock = new Mock<IClock>();

        var handler = new SaveAndExitRunCommandHandler(
            repository.Object, CreatePlayerProfileGateway().Object, clock.Object, Mock.Of<ILogger<SaveAndExitRunCommandHandler>>());

        var act = () => handler.Handle(
            new SaveAndExitRunCommand(unknownRunId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();

        repository.Verify(
            repo => repo.UpdateAsync(It.IsAny<Run>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunIsNotAtSafePoint()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Status.Should().Be(RunStatus.Active);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var handler = new SaveAndExitRunCommandHandler(
            repository.Object, CreatePlayerProfileGateway().Object, clock.Object, Mock.Of<ILogger<SaveAndExitRunCommandHandler>>());

        var act = () => handler.Handle(
            new SaveAndExitRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("*safe point*");

        repository.Verify(
            repo => repo.UpdateAsync(It.IsAny<Run>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenRunIsAlreadyClosed()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.Abandon(DateTimeOffset.UtcNow);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var handler = new SaveAndExitRunCommandHandler(
            repository.Object, CreatePlayerProfileGateway().Object, clock.Object, Mock.Of<ILogger<SaveAndExitRunCommandHandler>>());

        var act = () => handler.Handle(
            new SaveAndExitRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public async Task Handle_ShouldSyncNpcReputation_WhenRunHasRelationships()
    {
        // Regression: a suspended run never fires an integration event (unlike Abandon or
        // combat defeat), so any reputation gained before "Sauvegarder et quitter" used to be
        // silently orphaned if the player started a fresh run instead of resuming.
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.AdjustNpcRelationshipScore("npc.thomas", 5);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var playerProfileGateway = CreatePlayerProfileGateway();

        var handler = new SaveAndExitRunCommandHandler(
            repository.Object, playerProfileGateway.Object, clock.Object, Mock.Of<ILogger<SaveAndExitRunCommandHandler>>());

        await handler.Handle(
            new SaveAndExitRunCommand(run.Id.Value),
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

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenReputationSyncFails()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.AdjustNpcRelationshipScore("npc.thomas", 5);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var playerProfileGateway = new Mock<IPlayerProfileGateway>();
        playerProfileGateway
            .Setup(g => g.UpsertNpcReputationScoresAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<NpcReputationScoreView>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Player Service unreachable"));

        var handler = new SaveAndExitRunCommandHandler(
            repository.Object, playerProfileGateway.Object, clock.Object, Mock.Of<ILogger<SaveAndExitRunCommandHandler>>());

        var act = () => handler.Handle(
            new SaveAndExitRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
