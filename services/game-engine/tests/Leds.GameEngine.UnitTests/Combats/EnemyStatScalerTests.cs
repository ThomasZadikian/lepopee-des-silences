using FluentAssertions;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class EnemyStatScalerTests
{
    private readonly EnemyStatScaler _scaler = new();

    [Fact]
    public void Scale_ShouldKeepBaseStats_WhenMultiplierIsOne()
    {
        var result = _scaler.Scale(100, 10, 1.0);

        result.Vitality.Should().Be(100);
        result.Power.Should().Be(10);
        result.AppliedMultiplier.Should().Be(1.0);
    }

    [Fact]
    public void Scale_ShouldIncreaseVitality_WhenMultiplierIsAboveOne()
    {
        var result = _scaler.Scale(100, 10, 1.5);

        result.Vitality.Should().Be(150);
        result.Power.Should().Be(15);
    }

    [Fact]
    public void Scale_ShouldDecreaseStats_WhenMultiplierIsBelowOne()
    {
        var result = _scaler.Scale(100, 10, 0.8);

        result.Vitality.Should().Be(80);
        result.Power.Should().Be(8);
    }

    [Fact]
    public void Scale_ShouldNeverReturnStatsBelowOne()
    {
        var result = _scaler.Scale(1, 1, 0.1);

        result.Vitality.Should().BeGreaterThanOrEqualTo(1);
        result.Power.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Scale_ShouldClampMultiplier_WhenTooLow()
    {
        var result = _scaler.Scale(100, 10, 0.1);

        result.AppliedMultiplier.Should().Be(0.5);
        result.Vitality.Should().Be(50);
        result.Power.Should().Be(5);
    }

    [Fact]
    public void Scale_ShouldClampMultiplier_WhenTooHigh()
    {
        var result = _scaler.Scale(100, 10, 1500.0);

        result.AppliedMultiplier.Should().Be(1000.0);
        result.Vitality.Should().Be(100000);
        result.Power.Should().Be(10000);
    }

    [Fact]
    public void Scale_ShouldRoundStatsDeterministically()
    {
        var result = _scaler.Scale(100, 10, 1.33);

        result.Vitality.Should().Be(133);
        result.Power.Should().Be(14);
    }
}