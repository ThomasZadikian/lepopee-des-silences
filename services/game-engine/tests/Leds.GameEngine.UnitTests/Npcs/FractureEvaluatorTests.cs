using FluentAssertions;
using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.UnitTests.Npcs;

public sealed class FractureEvaluatorTests
{
    [Fact]
    public void Evaluate_ShouldReturnRompu_WhenScoreAtOrBelowRuptureThreshold()
    {
        FractureEvaluator.Evaluate(5, 30, 10).Should().Be(WoundState.Rompu);
        FractureEvaluator.Evaluate(10, 30, 10).Should().Be(WoundState.Rompu);
    }

    [Fact]
    public void Evaluate_ShouldReturnTendu_WhenScoreAboveRuptureButAtOrBelowTenseThreshold()
    {
        FractureEvaluator.Evaluate(11, 30, 10).Should().Be(WoundState.Tendu);
        FractureEvaluator.Evaluate(30, 30, 10).Should().Be(WoundState.Tendu);
    }

    [Fact]
    public void Evaluate_ShouldReturnLatent_WhenScoreAboveTenseThreshold()
    {
        FractureEvaluator.Evaluate(31, 30, 10).Should().Be(WoundState.Latent);
        FractureEvaluator.Evaluate(100, 30, 10).Should().Be(WoundState.Latent);
    }

    [Fact]
    public void Evaluate_ShouldHandleEqualThresholds()
    {
        FractureEvaluator.Evaluate(5, 20, 20).Should().Be(WoundState.Rompu);
        FractureEvaluator.Evaluate(20, 20, 20).Should().Be(WoundState.Rompu);
        FractureEvaluator.Evaluate(21, 20, 20).Should().Be(WoundState.Latent);
    }

    [Fact]
    public void Evaluate_ShouldHandleNegativeScores()
    {
        FractureEvaluator.Evaluate(-100, 30, 10).Should().Be(WoundState.Rompu);
    }

    [Fact]
    public void Evaluate_ShouldHandleRuptureGreaterThanTense()
    {
        // When ruptureThreshold > tenseThreshold, the Tendu range is effectively empty
        // because anything <= ruptureThreshold returns Rompu first.
        FractureEvaluator.Evaluate(15, 10, 20).Should().Be(WoundState.Rompu);
        FractureEvaluator.Evaluate(20, 10, 20).Should().Be(WoundState.Rompu);
        FractureEvaluator.Evaluate(21, 10, 20).Should().Be(WoundState.Latent);
    }
}
