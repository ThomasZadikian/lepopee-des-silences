using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.GetUpcomingRooms;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GetUpcomingRooms;

public sealed class GetUpcomingRoomsQueryHandlerTests
{
    private static PalaceLaw CreatePortesOuvertesLaw() => PalaceLaw.Create(
        "law.portes-ouvertes", "Édit des Portes Ouvertes", "1.0.0",
        domains: [PalaceLawDomain.Narrative],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.UpcomingRoomNamesRevealEnabled,
                value: 1,
                RunModifierDuration.UntilFloorEnds),
        ]);

    [Fact]
    public async Task Handle_ShouldReturnNotRevealed_WhenLawIsNotActive()
    {
        var run = TestGameEngineFactory.CreateRun();

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var runGenerator = new Mock<IRunGenerator>();
        var handler = new GetUpcomingRoomsQueryHandler(runRepo.Object, runGenerator.Object);

        var response = await handler.Handle(new GetUpcomingRoomsQuery(run.Id.Value), CancellationToken.None);

        response.RunId.Should().Be(run.Id.Value);
        response.IsRevealed.Should().BeFalse();
        response.Rooms.Should().BeEmpty();
        runGenerator.Verify(
            g => g.PreviewUpcomingRoomNamesAsync(It.IsAny<Run>(), It.IsAny<CancellationToken>()), Times.Never,
            "no point previewing anything when the law isn't active.");
    }

    [Fact]
    public async Task Handle_ShouldReturnThePreview_WhenLawIsActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.ActivatePalaceLaw(CreatePortesOuvertesLaw());

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var runGenerator = new Mock<IRunGenerator>();
        runGenerator
            .Setup(g => g.PreviewUpcomingRoomNamesAsync(run, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new UpcomingRoomPreview(1, "room.a", "Salle A"),
                new UpcomingRoomPreview(2, null, null),
            ]);

        var handler = new GetUpcomingRoomsQueryHandler(runRepo.Object, runGenerator.Object);

        var response = await handler.Handle(new GetUpcomingRoomsQuery(run.Id.Value), CancellationToken.None);

        response.IsRevealed.Should().BeTrue();
        response.Rooms.Should().HaveCount(2);
        response.Rooms.First().Key.Should().Be("room.a");
        response.Rooms.First().DisplayName.Should().Be("Salle A");
        response.Rooms.Last().Key.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(new RunId(runId), It.IsAny<CancellationToken>())).ReturnsAsync((Run?)null);

        var runGenerator = new Mock<IRunGenerator>();
        var handler = new GetUpcomingRoomsQueryHandler(runRepo.Object, runGenerator.Object);

        var act = () => handler.Handle(new GetUpcomingRoomsQuery(runId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Run*");
    }
}
