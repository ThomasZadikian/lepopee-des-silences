using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.UnitTests.Domain.Items;

public sealed class ItemVocabularyCatalogTests
{
    [Theory]
    [InlineData("common", ItemRarity.Common)]
    [InlineData("Legendary", ItemRarity.Legendary)]
    [InlineData(" UNIQUE ", ItemRarity.Unique)]
    public void ItemRarityCatalog_ShouldParseCodeAndEnumName(string value, ItemRarity expected)
    {
        ItemRarityCatalog.Parse(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void ItemRarityCatalog_ShouldRejectInvalidValues(string? value)
    {
        var act = () => ItemRarityCatalog.Parse(value!);
        act.Should().Throw<DomainException>();

        ItemRarityCatalog.TryParse(value, out _).Should().BeFalse();
    }

    [Fact]
    public void ItemRarityCatalog_ShouldExposeEveryDefinitionAndStableCodes()
    {
        ItemRarityCatalog.All.Should().HaveCount(6);
        foreach (var definition in ItemRarityCatalog.All)
        {
            ItemRarityCatalog.CodeOf(definition.Value).Should().Be(definition.Code);
            ItemRarityCatalog.TryParse(definition.Code, out var parsed).Should().BeTrue();
            parsed.Should().Be(definition.Value);
            definition.PalaceShardCost.Should().BeGreaterThanOrEqualTo(0);
            definition.HimLitShardCost.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void ItemRarityCatalog_ShouldRejectUnknownEnumValue()
    {
        var act = () => ItemRarityCatalog.CodeOf((ItemRarity)999);
        act.Should().Throw<DomainException>().WithMessage("*Unknown item rarity value*");
    }

    [Theory]
    [InlineData("consumable", ItemCategory.Consumable)]
    [InlineData("Equipment", ItemCategory.Equipment)]
    [InlineData(" WEATHERINSTRUMENT ", ItemCategory.WeatherInstrument)]
    public void ItemTypeCatalog_ShouldParseCodeAndEnumName(string value, ItemCategory expected)
    {
        ItemTypeCatalog.Parse(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void ItemTypeCatalog_ShouldRejectInvalidValues(string? value)
    {
        var act = () => ItemTypeCatalog.Parse(value!);
        act.Should().Throw<DomainException>();

        ItemTypeCatalog.TryParse(value, out _).Should().BeFalse();
    }

    [Fact]
    public void ItemTypeCatalog_ShouldExposeEveryDefinitionAndStableCodes()
    {
        ItemTypeCatalog.All.Should().HaveCount(10);
        foreach (var definition in ItemTypeCatalog.All)
        {
            ItemTypeCatalog.CodeOf(definition.Value).Should().Be(definition.Code);
            ItemTypeCatalog.TryParse(definition.Code, out var parsed).Should().BeTrue();
            parsed.Should().Be(definition.Value);
        }
    }

    [Fact]
    public void ItemTypeCatalog_ShouldRejectUnknownEnumValue()
    {
        var act = () => ItemTypeCatalog.CodeOf((ItemCategory)999);
        act.Should().Throw<DomainException>().WithMessage("*Unknown item category value*");
    }
}
