using FluentAssertions;
using Leds.Catalog.Application.Items.Dtos;
using Leds.Catalog.Application.Items.ListActiveItemTemplates;
using Leds.Catalog.Application.Items.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Items;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.Items;

public sealed class ListActiveItemTemplatesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedTemplates_WhenTemplatesExist()
    {
        var template = ItemTemplate.Create(
            "item.shadow-crystal",
            "Cristal d'Ombre",
            "Un cristal imprégné d'ombre pure.",
            "catalog-0.1.0",
            ItemCategory.Relic,
            ItemRarity.Rare,
            ItemDuration.Permanent,
            effectValue: 10,
            price: 50,
            status: CatalogContentStatus.Active);

        var readStore = Substitute.For<IItemTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IItemTemplate>>(new[] { template }));

        var handler = new ListActiveItemTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveItemTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().ContainSingle();

        var dto = response.Templates.Single();
        dto.Key.Should().Be("item.shadow-crystal");
        dto.Name.Should().Be("Cristal d'Ombre");
        dto.Status.Should().Be("Active");
        dto.Category.Should().Be("Relic");
        dto.Rarity.Should().Be("Rare");
        dto.Duration.Should().Be("Permanent");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoTemplatesExist()
    {
        var readStore = Substitute.For<IItemTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IItemTemplate>>(Array.Empty<IItemTemplate>()));

        var handler = new ListActiveItemTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveItemTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnMultipleTemplates_WhenMultipleExist()
    {
        var item1 = ItemTemplate.Create(
            "item-1", "Item 1", "Desc", "v1",
            ItemCategory.Consumable, ItemRarity.Common, ItemDuration.RunOnly,
            effectValue: 10, price: 5,
            status: CatalogContentStatus.Active);

        var item2 = ItemTemplate.Create(
            "item-2", "Item 2", "Desc", "v1",
            ItemCategory.Equipment, ItemRarity.Epic, ItemDuration.Permanent,
            effectValue: 15, price: 100,
            status: CatalogContentStatus.Active);

        var readStore = Substitute.For<IItemTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IItemTemplate>>(new[] { item1, item2 }));

        var handler = new ListActiveItemTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveItemTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().HaveCount(2);
        response.Templates.Should().Contain(t => t.Key == "item-1");
        response.Templates.Should().Contain(t => t.Key == "item-2");
    }
}
