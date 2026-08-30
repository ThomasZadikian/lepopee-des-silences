using FluentAssertions;
using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.UnitTests.Domain.Items;

public sealed class ItemCategoryTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)ItemCategory.Consumable).Should().Be(0);
        ((int)ItemCategory.Equipment).Should().Be(1);
        ((int)ItemCategory.Relic).Should().Be(2);
        ((int)ItemCategory.Key).Should().Be(3);
        ((int)ItemCategory.Currency).Should().Be(4);
        ((int)ItemCategory.Material).Should().Be(5);
        ((int)ItemCategory.Weapon).Should().Be(6);
        ((int)ItemCategory.Grimoire).Should().Be(7);
        ((int)ItemCategory.WeatherInstrument).Should().Be(8);
        ((int)ItemCategory.SkillEssence).Should().Be(9);
    }

    [Fact]
    public void ShouldHaveTenValues()
    {
        Enum.GetValues<ItemCategory>().Should().HaveCount(10);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<ItemCategory>("Relic");
        result.Should().Be(ItemCategory.Relic);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        ItemCategory.Consumable.ToString().Should().Be("Consumable");
    }

    [Fact]
    public void Consumable_ShouldBeDefaultValue()
    {
        default(ItemCategory).Should().Be(ItemCategory.Consumable);
    }
}
