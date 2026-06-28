using FluentAssertions;
using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.UnitTests.Domain.Items;

public sealed class ItemRarityTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)ItemRarity.Common).Should().Be(0);
        ((int)ItemRarity.Uncommon).Should().Be(1);
        ((int)ItemRarity.Rare).Should().Be(2);
        ((int)ItemRarity.Epic).Should().Be(3);
        ((int)ItemRarity.Legendary).Should().Be(4);
        ((int)ItemRarity.Unique).Should().Be(5);
    }

    [Fact]
    public void ShouldHaveSixValues()
    {
        Enum.GetValues<ItemRarity>().Should().HaveCount(6);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<ItemRarity>("Legendary");
        result.Should().Be(ItemRarity.Legendary);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        ItemRarity.Epic.ToString().Should().Be("Epic");
    }

    [Fact]
    public void Common_ShouldBeDefaultValue()
    {
        default(ItemRarity).Should().Be(ItemRarity.Common);
    }
}
