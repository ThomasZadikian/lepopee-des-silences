using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class PalacePublicIndicatorDtoTests
{
    [Theory]
    [InlineData("low", "low", null)]
    [InlineData("medium", "medium", null)]
    [InlineData("high", "high", null)]
    [InlineData("critical", "critical", null)]
    [InlineData("calm", null, "calm")]
    [InlineData("stable", null, "calm")]
    [InlineData("unstable", null, "unstable")]
    [InlineData("tense", null, "unstable")]
    [InlineData("danger", null, "danger")]
    [InlineData("dangerous", null, "danger")]
    [InlineData("mystery", null, "mystery")]
    [InlineData("unknown", null, "mystery")]
    [InlineData("  HIGH  ", "high", null)]
    [InlineData("custom", null, null)]
    public void FromDomain_ShouldNormalizeEveryPublicIntensity(
        string intensity,
        string? expectedLevel,
        string? expectedTone)
    {
        var indicator = PalaceIndicator.Create(
            Guid.NewGuid(),
            "indicator.test",
            "Test indicator",
            "Narrative",
            intensity);

        var dto = PalacePublicIndicatorDto.FromDomain(indicator);

        dto.Key.Should().Be("indicator.test");
        dto.Label.Should().Be("Test indicator");
        dto.Description.Should().Be("Narrative");
        dto.Level.Should().Be(expectedLevel);
        dto.Tone.Should().Be(expectedTone);
        dto.Source.Should().Be("run");
        dto.Category.Should().BeNull();
    }

    [Fact]
    public void FromDomain_ShouldRejectNullIndicator()
    {
        var act = () => PalacePublicIndicatorDto.FromDomain(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
