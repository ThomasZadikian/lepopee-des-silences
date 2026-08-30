using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Moq;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class ListAvailableCurseDefinitionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMapCatalogSnapshotsToCurseDefinitionViews()
    {
        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.ListAvailableCurseDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new CatalogCurseDefinitionSnapshot(
                    "curse.old-wound", "1.0", "Vieille blessure", "Rouvre une plaie ancienne.",
                    "Le passé ne se referme jamais tout à fait.", Severity: 3, Duration: "Permanent",
                    Trigger: "RunStart", EffectSetKey: null)
            });

        var handler = new ListAvailableCurseDefinitionsQueryHandler(catalogGateway.Object);

        var response = await handler.Handle(new ListAvailableCurseDefinitionsQuery(), CancellationToken.None);

        response.Curses.Should().ContainSingle();
        var curse = response.Curses.Single();
        curse.Key.Should().Be("curse.old-wound");
        curse.DisplayName.Should().Be("Vieille blessure");
        curse.Description.Should().Be("Rouvre une plaie ancienne.");
        curse.NarrativeText.Should().Be("Le passé ne se referme jamais tout à fait.");
        curse.Severity.Should().Be(3);
        curse.Duration.Should().Be("Permanent");
        curse.Trigger.Should().Be("RunStart");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenCatalogHasNoCurses()
    {
        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.ListAvailableCurseDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new ListAvailableCurseDefinitionsQueryHandler(catalogGateway.Object);

        var response = await handler.Handle(new ListAvailableCurseDefinitionsQuery(), CancellationToken.None);

        response.Curses.Should().BeEmpty();
    }
}
