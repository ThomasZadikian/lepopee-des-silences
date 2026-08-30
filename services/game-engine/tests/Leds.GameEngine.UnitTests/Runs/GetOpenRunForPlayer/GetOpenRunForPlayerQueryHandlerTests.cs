using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Runs.GetOpenRunForPlayer;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GetOpenRunForPlayer;

public sealed class GetOpenRunForPlayerQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnTheOpenRun_WhenTheBrowserHasLostItsLocalReference()
    {
        var run = TestGameEngineFactory.CreateRun();
        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetOpenByPlayerIdAsync(run.PlayerId, CancellationToken.None))
            .ReturnsAsync(run);
        var handler = new GetOpenRunForPlayerQueryHandler(repository.Object);

        var response = await handler.Handle(
            new GetOpenRunForPlayerQuery(run.PlayerId),
            CancellationToken.None);

        response.Run.Should().NotBeNull();
        response.Run!.Id.Should().Be(run.Id.Value);
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
    }

    [Fact]
    public async Task Handle_ShouldReturnNoRun_WhenThePlayerHasNoOpenRun()
    {
        var playerId = Guid.NewGuid();
        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetOpenByPlayerIdAsync(playerId, CancellationToken.None))
            .ReturnsAsync((Run?)null);
        var handler = new GetOpenRunForPlayerQueryHandler(repository.Object);

        var response = await handler.Handle(
            new GetOpenRunForPlayerQuery(playerId),
            CancellationToken.None);

        response.Run.Should().BeNull();
    }
}
