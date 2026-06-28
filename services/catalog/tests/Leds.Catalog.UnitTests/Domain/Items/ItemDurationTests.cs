using FluentAssertions;
using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.UnitTests.Domain.Items;

public sealed class ItemDurationTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)ItemDuration.RunOnly).Should().Be(0);
        ((int)ItemDuration.Permanent).Should().Be(1);
    }

    [Fact]
    public void ShouldHaveTwoValues()
    {
        Enum.GetValues<ItemDuration>().Should().HaveCount(2);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<ItemDuration>("Permanent");
        result.Should().Be(ItemDuration.Permanent);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        ItemDuration.RunOnly.ToString().Should().Be("RunOnly");
    }

    [Fact]
    public void RunOnly_ShouldBeDefaultValue()
    {
        default(ItemDuration).Should().Be(ItemDuration.RunOnly);
    }
}
