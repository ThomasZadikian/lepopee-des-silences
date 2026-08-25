using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.AbandonRun;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.AbandonRun;

public sealed class AbandonRunCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldAbandonRun_AndPersistIt()
    {
        // Arrange — a normal active run can be abandoned.
        var run = TestGameEngineFactory.CreateRun();
        run.Status.Should().Be(RunStatus.Active);

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
            Mock.Of<IOutboxWriter>(),
            clock.Object);

        // Act
        var response = await handler.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None);

        // Assert
        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.Status.Should().Be(RunStatus.Resolved.ToString());
        response.Run.Outcome.Should().Be(RunOutcome.Abandon.ToString());

        run.Status.Should().Be(RunStatus.Resolved);
        run.Outcome.Should().Be(RunOutcome.Abandon);
        run.EndedAt.Should().Be(now);

        repository.Verify(
            repo => repo.UpdateAsync(
                It.Is<Run>(candidate =>
                    candidate.Id == run.Id &&
                    candidate.Status == RunStatus.Resolved &&
                    candidate.Outcome == RunOutcome.Abandon &&
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
            Mock.Of<IOutboxWriter>(),
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
    public async Task Handle_ShouldAbandonRun_WhenRunIsNotAtSafePoint()
    {
        // Arrange — room mid node-selection: abandonment must still recover the account.
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(
            Leds.GameEngine.Domain.Nodes.NodeEventType.Item).Run;

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
            Mock.Of<IOutboxWriter>(),
            clock.Object);

        // Act
        var response = await handler.Handle(
            new AbandonRunCommand(run.Id.Value),
            CancellationToken.None);

        // Assert — abandonment is the recovery escape hatch and must close any open run.
        response.Run.Status.Should().Be(RunStatus.Resolved.ToString());
        response.Run.Outcome.Should().Be(RunOutcome.Abandon.ToString());

        repository.Verify(
            repo => repo.UpdateAsync(
                It.Is<Run>(candidate =>
                    candidate.Status == RunStatus.Resolved &&
                    candidate.Outcome == RunOutcome.Abandon),
                CancellationToken.None),
            Times.Once);
    }
}
