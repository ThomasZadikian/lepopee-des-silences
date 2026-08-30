using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Protocol;
using Leds.GameEngine.Application.Runs.MoveParty;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.MoveParty;

public sealed class MovePartyCommandHandlerTests
{
    private static LocalRuleProtocolEvaluator CreateLocalRuleProtocolEvaluator() =>
        new(new HardcodedLocalRuleProvider());

    [Fact]
    public async Task Handle_ShouldMoveTheParty_AndPersistTheRun()
    {
        var run = TestGameEngineFactory.CreateGridRun();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new MovePartyCommandHandler(repo.Object, CreateLocalRuleProtocolEvaluator());

        var response = await handler.Handle(
            new MovePartyCommand(run.Id.Value, 1, 0),
            CancellationToken.None);

        run.CurrentRoom.Grid!.PartyX.Should().Be(1);
        run.CurrentRoom.Grid.PartyY.Should().Be(0);
        response.Run.Should().NotBeNull();
        response.LocalRuleNotices.Should().BeEmpty("this test room has no authored LocalRule protocol");
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new MovePartyCommandHandler(repo.Object, CreateLocalRuleProtocolEvaluator());

        var act = () => handler.Handle(
            new MovePartyCommand(Guid.NewGuid(), 1, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
