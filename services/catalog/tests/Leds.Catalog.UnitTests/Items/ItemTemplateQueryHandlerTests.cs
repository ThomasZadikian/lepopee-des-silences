using FluentAssertions;
using Leds.Catalog.Application.Items.GetItemTemplateByKey;
using Leds.Catalog.Application.Items.ListActiveItemTemplates;
using Leds.Catalog.Application.Items.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Items;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Items;

public sealed class ItemTemplateQueryHandlerTests
{
    [Fact]
    public async Task GetByKey_ShouldReturnTemplate_WhenTemplateExists()
    {
        var template = CreateItemTemplate();

        var readStore = Substitute.For<IItemTemplateReadStore>();
        readStore.GetByKeyAsync("item-memory-potion", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IItemTemplate?>(template));

        var handler = new GetItemTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetItemTemplateByKeyQuery("item-memory-potion"),
            CancellationToken.None);

        response.Template.Should().NotBeNull();
        response.Template!.Key.Should().Be("item-memory-potion");
        response.Template.Name.Should().Be("Potion de Mémoire");
        response.Template.Category.Should().Be("Consumable");
        response.Template.Rarity.Should().Be("Common");
        response.Template.Duration.Should().Be("RunOnly");
    }

    [Fact]
    public async Task GetByKey_ShouldReturnNullTemplate_WhenTemplateDoesNotExist()
    {
        var readStore = Substitute.For<IItemTemplateReadStore>();
        readStore.GetByKeyAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IItemTemplate?>(null));

        var handler = new GetItemTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetItemTemplateByKeyQuery("unknown"),
            CancellationToken.None);

        response.Template.Should().BeNull();
    }

    [Fact]
    public async Task ListActive_ShouldReturnMappedTemplates()
    {
        var template = CreateItemTemplate();

        var readStore = Substitute.For<IItemTemplateReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IItemTemplate>>(
                new[] { template }));

        var handler = new ListActiveItemTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveItemTemplatesQuery(),
            CancellationToken.None);

        response.Templates.Should().ContainSingle();

        var dto = response.Templates.Single();

        dto.Key.Should().Be("item-memory-potion");
        dto.Name.Should().Be("Potion de Mémoire");
        dto.Status.Should().Be("Active");
    }

    private static ItemTemplate CreateItemTemplate()
    {
        return ItemTemplate.Create(
            "item-memory-potion",
            "Potion de Mémoire",
            "Restaure une ressource mentale pendant la run.",
            "catalog-0.1.0",
            ItemCategory.Consumable,
            ItemRarity.Common,
            ItemDuration.RunOnly,
            effectValue: 25,
            price: 10,
            status: CatalogContentStatus.Active);
    }
}