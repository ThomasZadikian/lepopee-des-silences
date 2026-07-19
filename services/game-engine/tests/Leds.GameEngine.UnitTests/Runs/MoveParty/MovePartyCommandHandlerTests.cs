using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.MoveParty;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.MoveParty;

public sealed class MovePartyCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMoveTheParty_AndPersistTheRun()
    {
        var run = TestGameEngineFactory.CreateGridRun();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new MovePartyCommandHandler(repo.Object);

        var response = await handler.Handle(
            new MovePartyCommand(run.Id.Value, 1, 0),
            CancellationToken.None);

        run.CurrentRoom.Grid!.PartyX.Should().Be(1);
        run.CurrentRoom.Grid.PartyY.Should().Be(0);
        response.Run.Should().NotBeNull();
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new MovePartyCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new MovePartyCommand(Guid.NewGuid(), 1, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_OnAClassicRun()
    {
        var run = TestGameEngineFactory.CreateRun();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new MovePartyCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new MovePartyCommand(run.Id.Value, 1, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
