using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.ChallengeBossRemotely;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ChallengeBossRemotely;

public sealed class ChallengeBossRemotelyCommandHandlerTests
{
    private static Run CreateRunWithExhaustedBudget()
    {
        var room = TestGameEngineFactory.CreateGridThresholdRoom(movementBudget: 8).Room;

        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-unit-test-grid-remote-boss-handler",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.MoveParty(4, 0); // costs 4, remaining 4
        run.MoveParty(0, 0); // costs 4 back to start, remaining 0

        return run;
    }

    [Fact]
    public async Task Handle_ShouldSelectTheBoss_AndPersistTheRun()
    {
        var run = CreateRunWithExhaustedBudget();
        var boss = run.CurrentRoom.Nodes.Single(n => n.IsBoss);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new ChallengeBossRemotelyCommandHandler(repo.Object);

        var response = await handler.Handle(
            new ChallengeBossRemotelyCommand(run.Id.Value),
            CancellationToken.None);

        boss.State.Should().Be(NodeState.Selected);
        run.CurrentRoom.State.Should().Be(RoomState.NodeSelected);
        response.Run.Should().NotBeNull();
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new ChallengeBossRemotelyCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new ChallengeBossRemotelyCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenBudgetStillRemains()
    {
        var run = TestGameEngineFactory.CreateGridRun();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new ChallengeBossRemotelyCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new ChallengeBossRemotelyCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
