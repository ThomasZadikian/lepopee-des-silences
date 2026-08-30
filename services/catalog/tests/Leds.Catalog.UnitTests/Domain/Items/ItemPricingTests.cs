using FluentAssertions;
using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.UnitTests.Domain.Items;

public sealed class ItemPricingTests
{
    [Theory]
    [InlineData(ItemRarity.Common, 150, 0)]
    [InlineData(ItemRarity.Uncommon, 250, 0)]
    [InlineData(ItemRarity.Rare, 350, 0)]
    [InlineData(ItemRarity.Epic, 500, 25)]
    [InlineData(ItemRarity.Legendary, 750, 50)]
    [InlineData(ItemRarity.Unique, 1000, 75)]
    public void ForRarity_ShouldReturnFixedCost_ForEachTier(
        ItemRarity rarity, int expectedPalaceShardCost, int expectedHimLitShardCost)
    {
        var (palaceShardCost, himLitShardCost) = ItemPricing.ForRarity(rarity);

        palaceShardCost.Should().Be(expectedPalaceShardCost);
        himLitShardCost.Should().Be(expectedHimLitShardCost);
    }

    [Theory]
    [InlineData("common", 150, 0)]
    [InlineData("UNIQUE", 1000, 75)]
    [InlineData("Legendary", 750, 50)]
    public void ForRarity_ShouldParseStringCaseInsensitively(
        string rarity, int expectedPalaceShardCost, int expectedHimLitShardCost)
    {
        var (palaceShardCost, himLitShardCost) = ItemPricing.ForRarity(rarity);

        palaceShardCost.Should().Be(expectedPalaceShardCost);
        himLitShardCost.Should().Be(expectedHimLitShardCost);
    }

    [Fact]
    public void ForRarity_ShouldThrow_ForUnknownEnumValue()
    {
        var action = () => ItemPricing.ForRarity((ItemRarity)999);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ForRarity_ShouldThrow_ForUnknownStringValue()
    {
        var action = () => ItemPricing.ForRarity("NotARarity");

        action.Should().Throw<ArgumentException>();
    }
}
