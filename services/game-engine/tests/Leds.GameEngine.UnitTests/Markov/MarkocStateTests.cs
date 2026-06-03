using FluentAssertions;
using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.UnitTests.Markov;

public sealed class MarkovStateTests
{
    [Fact]
    public void Create_ShouldTrimValue()
    {
        var state = MarkovState.Create("  Wary  ");

        state.Value.Should().Be("Wary");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_ShouldThrow_WhenValueIsBlank(string value)
    {
        var act = () => MarkovState.Create(value);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Markov state value is required.");
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitive()
    {
        var first = MarkovState.Create("Wary");
        var second = MarkovState.Create("wary");

        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}