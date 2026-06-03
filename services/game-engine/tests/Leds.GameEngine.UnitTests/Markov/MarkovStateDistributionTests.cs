using FluentAssertions;
using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.UnitTests.Markov;

public sealed class MarkovStateDistributionTests
{
    [Fact]
    public void Create_ShouldThrow_WhenDistributionIsEmpty()
    {
        var act = () => MarkovStateDistribution.Create(
            new Dictionary<MarkovState, decimal>());

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Markov state distribution cannot be empty.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDistributionDoesNotSumToOne()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");

        var act = () => MarkovStateDistribution.Create(
            new Dictionary<MarkovState, decimal>
            {
                [calm] = 0.4m,
                [wary] = 0.4m
            });

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Markov state distribution probabilities must sum to 1. Actual sum is 0.8.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenProbabilityIsGreaterThanOne()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");

        var act = () => MarkovStateDistribution.Create(
            new Dictionary<MarkovState, decimal>
            {
                [calm] = 1.2m,
                [wary] = -0.2m
            });

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Distribution probability for state 'Calm' must be between 0 and 1.");
    }

    [Fact]
    public void FromSingleState_ShouldCreateDiracDistribution()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");

        var distribution = MarkovStateDistribution.FromSingleState(
            new[] { calm, wary },
            wary);

        distribution[calm].Should().Be(0m);
        distribution[wary].Should().Be(1m);
    }

    [Fact]
    public void FromSingleState_ShouldThrow_WhenActiveStateIsUnknown()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");

        var act = () => MarkovStateDistribution.FromSingleState(
            new[] { calm },
            wary);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Active state 'Wary' is not part of the Markov state set.");
    }
}