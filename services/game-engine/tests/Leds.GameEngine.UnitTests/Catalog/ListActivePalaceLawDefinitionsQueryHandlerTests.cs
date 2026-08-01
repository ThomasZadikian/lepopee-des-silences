using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Moq;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class ListActivePalaceLawDefinitionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMapCatalogSnapshotsToPalaceLawDefinitionViews()
    {
        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.ListActivePalaceLawDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PalaceLawDefinitionSnapshot(
                    "law-aegis-v1", "Aegis", "Une loi de protection.", "1.0", "Active", "Visible",
                    Priority: 1, ImpactDomains: ["Combat"], Rarity: "Rare", Polarity: "Positif", IsMajeure: true)
            });

        var handler = new ListActivePalaceLawDefinitionsQueryHandler(catalogGateway.Object);

        var response = await handler.Handle(new ListActivePalaceLawDefinitionsQuery(), CancellationToken.None);

        response.Laws.Should().ContainSingle();
        var law = response.Laws.Single();
        law.Key.Should().Be("law-aegis-v1");
        law.Name.Should().Be("Aegis");
        law.Description.Should().Be("Une loi de protection.");
        law.Rarity.Should().Be("Rare");
        law.Polarity.Should().Be("Positif");
        law.IsMajeure.Should().BeTrue();
        law.ImpactDomains.Should().ContainSingle().Which.Should().Be("Combat");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenCatalogHasNoLaws()
    {
        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.ListActivePalaceLawDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new ListActivePalaceLawDefinitionsQueryHandler(catalogGateway.Object);

        var response = await handler.Handle(new ListActivePalaceLawDefinitionsQuery(), CancellationToken.None);

        response.Laws.Should().BeEmpty();
    }
}
