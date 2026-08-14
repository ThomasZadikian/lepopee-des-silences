using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.UnitTests.Npcs;

public sealed class LieDetectorTests
{
    [Fact]
    public void Assess_ShouldBelieve_WhenSuspicionBelowDoubtThreshold()
    {
        var result = LieDetector.Assess(suspicionScore: 1, doubtThreshold: 3, detectionThreshold: 6, contradictsConfirmedFact: false);

        result.Should().Be(LieAssessment.Believed);
    }

    [Fact]
    public void Assess_ShouldDoubt_WhenSuspicionBetweenThresholds()
    {
        var result = LieDetector.Assess(suspicionScore: 4, doubtThreshold: 3, detectionThreshold: 6, contradictsConfirmedFact: false);

        result.Should().Be(LieAssessment.Doubted);
    }

    [Fact]
    public void Assess_ShouldDetect_WhenSuspicionAtOrAboveDetectionThreshold()
    {
        var result = LieDetector.Assess(suspicionScore: 6, doubtThreshold: 3, detectionThreshold: 6, contradictsConfirmedFact: false);

        result.Should().Be(LieAssessment.Detected);
    }

    [Fact]
    public void Assess_ShouldAlwaysDetect_WhenClaimContradictsAConfirmedFact_RegardlessOfSuspicion()
    {
        var result = LieDetector.Assess(suspicionScore: 0, doubtThreshold: 3, detectionThreshold: 6, contradictsConfirmedFact: true);

        result.Should().Be(LieAssessment.Detected);
    }

    [Fact]
    public void Assess_ShouldThrow_WhenDoubtThresholdExceedsDetectionThreshold()
    {
        var act = () => LieDetector.Assess(0, doubtThreshold: 7, detectionThreshold: 6, contradictsConfirmedFact: false);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Assess_ShouldThrow_OnNegativeThresholds()
    {
        var act = () => LieDetector.Assess(0, doubtThreshold: -1, detectionThreshold: 6, contradictsConfirmedFact: false);

        act.Should().Throw<DomainException>();
    }
}
