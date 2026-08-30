using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.RemovePalaceLaw;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.RemovePalaceLaw;

public sealed class RemovePalaceLawCommandHandlerTests
{
    private static PalaceLaw CreateLaw(string key = "law-echo-v1") => PalaceLaw.Create(
        key, "Loi de l'Écho", "1.0.0",
        domains: [PalaceLawDomain.Combat],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.PermanentCombatDifficultyBonus,
                value: 0.10,
                RunModifierDuration.UntilRunEnds),
        ]);

    [Fact]
    public async Task Handle_ShouldRemoveTheLaw_AndPersistTheRun()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: true);
        var law = CreateLaw();
        run.ActivatePalaceLaw(law);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new RemovePalaceLawCommandHandler(repo.Object);

        var response = await handler.Handle(
            new RemovePalaceLawCommand(run.Id.Value, law.Key),
            CancellationToken.None);

        response.Run.ActivePalaceLaws.Should().NotContain(activeLaw => activeLaw.Key == law.Key);
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new RemovePalaceLawCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new RemovePalaceLawCommand(Guid.NewGuid(), "law-echo-v1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenLawDenialNotEnabled()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: false);
        var law = CreateLaw();
        run.ActivatePalaceLaw(law);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new RemovePalaceLawCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new RemovePalaceLawCommand(run.Id.Value, law.Key),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
