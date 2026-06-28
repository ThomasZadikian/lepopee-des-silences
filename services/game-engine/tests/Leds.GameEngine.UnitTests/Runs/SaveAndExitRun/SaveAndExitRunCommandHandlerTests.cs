using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.SaveAndExitRun;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.SaveAndExitRun;

public sealed class SaveAndExitRunCommandHandlerTests
{
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

        var handler = new SaveAndExitRunCommandHandler(repository.Object, clock.Object);

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

        var handler = new SaveAndExitRunCommandHandler(repository.Object, clock.Object);

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

        var handler = new SaveAndExitRunCommandHandler(repository.Object, clock.Object);

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

        var handler = new SaveAndExitRunCommandHandler(repository.Object, clock.Object);

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

        var handler = new SaveAndExitRunCommandHandler(repository.Object, clock.Object);

        var act = () => handler.Handle(
            new SaveAndExitRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("*closed*");
    }
}
