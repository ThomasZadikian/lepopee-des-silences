using FluentAssertions;
using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.UnitTests.PalaceLaws;

public sealed class AdaptiveInfluenceTests
{
    [Fact]
    public void Create_ShouldSetAllFields()
    {
        var influence = AdaptiveInfluence.Create(
            Guid.NewGuid(), "PalaceLaw", "law-silence-v1",
            "ModifyEnemyBias", "enemy.paranoid", 0.15m, "Percent", "UntilRunEnds");

        influence.SourceType.Should().Be("PalaceLaw");
        influence.SourceKey.Should().Be("law-silence-v1");
        influence.InfluenceType.Should().Be("ModifyEnemyBias");
        influence.InfluenceTag.Should().Be("enemy.paranoid");
        influence.Value.Should().Be(0.15m);
        influence.ValueMode.Should().Be("Percent");
        influence.Duration.Should().Be("UntilRunEnds");
        influence.IsConsumed.Should().BeFalse();
    }

    [Fact]
    public void Consume_ShouldSetConsumedAtUtc()
    {
        var influence = AdaptiveInfluence.Create(
            Guid.NewGuid(), "Curse", "curse-1", "ApplyBehaviorTag", "behavior.hostile");
        var consumedAt = DateTime.UtcNow;
        influence.Consume(consumedAt);
        influence.ConsumedAtUtc.Should().Be(consumedAt);
        influence.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldThrow_WhenSourceTypeEmpty()
    {
        var act = () => AdaptiveInfluence.Create(
            Guid.NewGuid(), "", "key", "type", "tag");
        act.Should().Throw<Leds.GameEngine.Domain.Common.DomainException>();
    }
}

public sealed class PalaceIndicatorTests
{
    [Fact]
    public void Create_ShouldSetAllFields()
    {
        var indicator = PalaceIndicator.Create(
            Guid.NewGuid(), "pressure.hostile", "Pression hostile",
            "Le Palais se crispe.", "High");

        indicator.IndicatorKey.Should().Be("pressure.hostile");
        indicator.DisplayLabel.Should().Be("Pression hostile");
        indicator.NarrativeText.Should().Be("Le Palais se crispe.");
        indicator.Intensity.Should().Be("High");
        indicator.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldBeTrue_WhenExpiresAtUtcPassed()
    {
        var indicator = PalaceIndicator.Create(
            Guid.NewGuid(), "test", "Test", "Test text", "Low",
            expiresAtUtc: DateTime.UtcNow.AddHours(-1));

        indicator.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldThrow_WhenIndicatorKeyEmpty()
    {
        var act = () => PalaceIndicator.Create(
            Guid.NewGuid(), "", "Label", "Text", "Low");
        act.Should().Throw<Leds.GameEngine.Domain.Common.DomainException>();
    }
}
