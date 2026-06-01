using FluentAssertions;
using Leds.Catalog.Domain;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.UnitTests.Items;

public sealed class ItemTemplateTests
{
    [Fact]
    public void Create_ShouldCreateItemTemplate_WhenInputIsValid()
    {
        var item = ItemTemplate.Create(
            "item-memory-potion",
            "Potion de Mémoire",
            "Restaure une ressource mentale pendant la run.",
            "catalog-0.1.0",
            ItemCategory.Consumable,
            ItemRarity.Common,
            ItemDuration.RunOnly,
            effectValue: 25,
            price: 10);

        item.Id.Value.Should().NotBeEmpty();
        item.Key.Value.Should().Be("item-memory-potion");
        item.Name.Value.Should().Be("Potion de Mémoire");
        item.Description.Value.Should().Be("Restaure une ressource mentale pendant la run.");
        item.Version.Value.Should().Be("catalog-0.1.0");
        item.Status.Should().Be(CatalogContentStatus.Draft);
        item.Category.Should().Be(ItemCategory.Consumable);
        item.Rarity.Should().Be(ItemRarity.Common);
        item.Duration.Should().Be(ItemDuration.RunOnly);
        item.EffectValue.Should().Be(25);
        item.Price.Should().Be(10);
    }

    [Fact]
    public void Create_ShouldCreatePermanentRelic_WhenInputIsValid()
    {
        var item = ItemTemplate.Create(
            "item-silent-relic",
            "Relique du Silence",
            "Une trace permanente du Palais.",
            "catalog-0.1.0",
            ItemCategory.Relic,
            ItemRarity.Rare,
            ItemDuration.Permanent,
            effectValue: 1,
            price: 0);

        item.Category.Should().Be(ItemCategory.Relic);
        item.Rarity.Should().Be(ItemRarity.Rare);
        item.Duration.Should().Be(ItemDuration.Permanent);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenEffectValueIsNegative()
    {
        var act = () => ItemTemplate.Create(
            "item-memory-potion",
            "Potion de Mémoire",
            "Description",
            "catalog-0.1.0",
            ItemCategory.Consumable,
            ItemRarity.Common,
            ItemDuration.RunOnly,
            effectValue: -1,
            price: 10);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Item effect value cannot be negative.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenPriceIsNegative()
    {
        var act = () => ItemTemplate.Create(
            "item-memory-potion",
            "Potion de Mémoire",
            "Description",
            "catalog-0.1.0",
            ItemCategory.Consumable,
            ItemRarity.Common,
            ItemDuration.RunOnly,
            effectValue: 25,
            price: -1);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Item price cannot be negative.");
    }
}