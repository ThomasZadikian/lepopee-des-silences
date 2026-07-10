using FluentAssertions;
using Leds.Catalog.Application.Items.Definitions.Dtos;
using Leds.Catalog.Application.Items.Definitions.ListActiveItemDefinitions;
using Leds.Catalog.Application.Items.Definitions.Ports;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Items;

public sealed class ListActiveItemDefinitionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDefinitions_WhenTheyExist()
    {
        var dto = new ItemDefinitionDto(
            Guid.NewGuid(), "canon.item.monocle-pomenian", "canon-1.0.0", "Le monocle de Pomenian",
            "Une lentille gravée.", null, "Equipment", "Accessory", "Epic",
            "NotUsable", "PersistentMeta", "Additive", 1, false, false, null, "Active",
            IsPermanentEligible: true, EquipmentEffects: []);

        var readStore = Substitute.For<IItemDefinitionReadStore>();
        readStore.ListActiveDtosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ItemDefinitionDto>>(new[] { dto }));

        var handler = new ListActiveItemDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(new ListActiveItemDefinitionsQuery(), CancellationToken.None);

        response.Definitions.Should().ContainSingle();
        response.Definitions.Single().Key.Should().Be("canon.item.monocle-pomenian");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoDefinitionsExist()
    {
        var readStore = Substitute.For<IItemDefinitionReadStore>();
        readStore.ListActiveDtosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ItemDefinitionDto>>(Array.Empty<ItemDefinitionDto>()));

        var handler = new ListActiveItemDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(new ListActiveItemDefinitionsQuery(), CancellationToken.None);

        response.Definitions.Should().BeEmpty();
    }
}
