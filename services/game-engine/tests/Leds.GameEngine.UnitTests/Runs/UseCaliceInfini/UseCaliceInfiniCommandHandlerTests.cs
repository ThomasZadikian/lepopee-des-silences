using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.UseCaliceInfini;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.UseCaliceInfini;

public sealed class UseCaliceInfiniCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldHealThePlayer_AndPersistTheRun()
    {
        var run = TestGameEngineFactory.CreateRun(caliceInfiniEnabled: true);
        run.PlayerState.TakeDamage(run.PlayerState.MaxVitality - 1);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new UseCaliceInfiniCommandHandler(repo.Object);

        var response = await handler.Handle(
            new UseCaliceInfiniCommand(run.Id.Value, TargetCombatantId: null),
            CancellationToken.None);

        response.Run.PlayerState!.CurrentVitality.Should().BeGreaterThan(1);
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new UseCaliceInfiniCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new UseCaliceInfiniCommand(Guid.NewGuid(), TargetCombatantId: null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenCaliceInfiniNotEnabled()
    {
        var run = TestGameEngineFactory.CreateRun(caliceInfiniEnabled: false);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new UseCaliceInfiniCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new UseCaliceInfiniCommand(run.Id.Value, TargetCombatantId: null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
