using FluentAssertions;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.UnitTests.Rewards;

public sealed class RewardPowerScalerTests
{
    private readonly RewardPowerScaler _scaler = new();

    [Fact]
    public void ScaleAmount_ShouldKeepBaseAmount_WhenMultiplierIsOne()
    {
        var result = _scaler.ScaleAmount(100, 1.0);

        result.Should().Be(100);
    }

    [Fact]
    public void ScaleAmount_ShouldIncreaseAmount_WhenMultiplierIsAboveOne()
    {
        var result = _scaler.ScaleAmount(100, 1.5);

        result.Should().Be(150);
    }

    [Fact]
    public void ScaleAmount_ShouldDecreaseAmount_WhenMultiplierIsBelowOne()
    {
        var result = _scaler.ScaleAmount(100, 0.8);

        result.Should().Be(80);
    }

    [Fact]
    public void ScaleAmount_ShouldNeverReturnAmountBelowOne()
    {
        var result = _scaler.ScaleAmount(1, 0.1);

        result.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void ScaleAmount_ShouldClampMultiplier_WhenTooLow()
    {
        var result = _scaler.ScaleAmount(100, 0.1);

        result.Should().Be(50);
    }

    [Fact]
    public void ScaleAmount_ShouldClampMultiplier_WhenTooHigh()
    {
        var result = _scaler.ScaleAmount(100, 5.0);

        result.Should().Be(300);
    }

    [Fact]
    public void ScaleAmount_ShouldRoundDeterministically()
    {
        var result = _scaler.ScaleAmount(100, 1.33);

        result.Should().Be(133);
    }
}