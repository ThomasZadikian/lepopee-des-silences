using FluentAssertions;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Npcs;

public sealed class EmotionalRegisterTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        ((int)EmotionalRegister.Neutral).Should().Be(0);
        ((int)EmotionalRegister.Effroi).Should().Be(1);
        ((int)EmotionalRegister.Deni).Should().Be(2);
        ((int)EmotionalRegister.Melancolie).Should().Be(3);
        ((int)EmotionalRegister.Rupture).Should().Be(4);
        ((int)EmotionalRegister.Memoire).Should().Be(5);
        ((int)EmotionalRegister.Silence).Should().Be(6);
    }

    [Fact]
    public void ShouldHaveSevenValues()
    {
        Enum.GetValues<EmotionalRegister>().Should().HaveCount(7);
    }

    [Fact]
    public void ShouldParseFromString_WhenValidValueProvided()
    {
        var result = Enum.Parse<EmotionalRegister>("Rupture");
        result.Should().Be(EmotionalRegister.Rupture);
    }

    [Fact]
    public void ShouldReturnCorrectName_WhenToStringCalled()
    {
        EmotionalRegister.Melancolie.ToString().Should().Be("Melancolie");
    }

    [Fact]
    public void Neutral_ShouldBeDefaultValue()
    {
        default(EmotionalRegister).Should().Be(EmotionalRegister.Neutral);
    }
}
