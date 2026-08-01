using FluentAssertions;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.UnitTests.Rewards;

public sealed class ItemPricingTests
{
    [Theory]
    [InlineData("Common", 150, 0)]
    [InlineData("Uncommon", 250, 0)]
    [InlineData("Rare", 350, 0)]
    [InlineData("Epic", 500, 25)]
    [InlineData("Legendary", 750, 50)]
    [InlineData("Unique", 1000, 75)]
    public void ForRarity_ShouldReturnFixedCost_ForEachTier(
        string rarity, int expectedPalaceShardCost, int expectedHimLitShardCost)
    {
        var (palaceShardCost, himLitShardCost) = ItemPricing.ForRarity(rarity);

        palaceShardCost.Should().Be(expectedPalaceShardCost);
        himLitShardCost.Should().Be(expectedHimLitShardCost);
    }

    [Theory]
    [InlineData("common")]
    [InlineData("UNIQUE")]
    [InlineData("  Epic  ")]
    public void ForRarity_ShouldBeCaseAndWhitespaceInsensitive(string rarity)
    {
        var act = () => ItemPricing.ForRarity(rarity);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NotARarity")]
    public void ForRarity_ShouldDefaultToCommonPricing_ForUnknownOrMissingRarity(string? rarity)
    {
        var (palaceShardCost, himLitShardCost) = ItemPricing.ForRarity(rarity);

        palaceShardCost.Should().Be(150);
        himLitShardCost.Should().Be(0);
    }
}
