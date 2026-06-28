using FluentAssertions;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Npcs;

public sealed class WoundStateTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)WoundState.Latent).Should().Be(0);
        ((int)WoundState.Tendu).Should().Be(1);
        ((int)WoundState.Rompu).Should().Be(2);
    }

    [Fact]
    public void ShouldHaveThreeValues()
    {
        Enum.GetValues<WoundState>().Should().HaveCount(3);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<WoundState>("Rompu");
        result.Should().Be(WoundState.Rompu);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        WoundState.Tendu.ToString().Should().Be("Tendu");
    }

    [Fact]
    public void Latent_ShouldBeDefaultValue()
    {
        default(WoundState).Should().Be(WoundState.Latent);
    }
}
