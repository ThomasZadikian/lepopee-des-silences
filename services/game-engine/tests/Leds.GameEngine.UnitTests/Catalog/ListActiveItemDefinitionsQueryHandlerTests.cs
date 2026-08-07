using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Moq;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class ListActiveItemDefinitionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMapCatalogSnapshotsToItemDefinitionViews()
    {
        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.ListActiveItemDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new CatalogItemDefinitionSnapshot(
                    "canon.item.monocle-pomenian", "canon-1.0.0", "Le monocle de Pomenian",
                    "Une lentille gravée.", null, "Equipment", "Accessory", "Epic",
                    "NotUsable", "PersistentMeta", "Additive", 1, false, false,
                    IsPermanentEligible: true,
                    EquipmentEffects:
                    [
                        new CatalogItemEquipmentEffect(
                            "StatModifier", "Focus", 4, null, null)
                    ],
                    EffectValue: 0,
                    EffectRunType: null,
                    TacticalRange: 2,
                    TacticalAreaShape: "Diamond",
                    RequiresLineOfSight: true,
                    PalaceShardCost: 500,
                    HimLitShardCost: 25)
            });

        var handler = new ListActiveItemDefinitionsQueryHandler(catalogGateway.Object);

        var response = await handler.Handle(new ListActiveItemDefinitionsQuery(), CancellationToken.None);

        response.Items.Should().ContainSingle();
        var item = response.Items.Single();
        item.Key.Should().Be("canon.item.monocle-pomenian");
        item.DisplayName.Should().Be("Le monocle de Pomenian");
        item.Category.Should().Be("Equipment");
        item.FlavorTag.Should().Be("Accessory");
        item.EquipSlot.Should().Be("Accessory");
        item.Rarity.Should().Be("Epic");
        item.TacticalRange.Should().Be(2);
        item.TacticalAreaShape.Should().Be("Diamond");
        item.RequiresLineOfSight.Should().BeTrue();
        item.PalaceShardCost.Should().Be(500);
        item.HimLitShardCost.Should().Be(25);
        item.EquipmentEffects.Should().ContainSingle()
            .Which.StatKind.Should().Be("Focus");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenCatalogHasNoItems()
    {
        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.ListActiveItemDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new ListActiveItemDefinitionsQueryHandler(catalogGateway.Object);

        var response = await handler.Handle(new ListActiveItemDefinitionsQuery(), CancellationToken.None);

        response.Items.Should().BeEmpty();
    }
}
