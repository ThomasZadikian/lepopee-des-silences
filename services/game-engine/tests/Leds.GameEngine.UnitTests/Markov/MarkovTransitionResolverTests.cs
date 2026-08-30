using FluentAssertions;
using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.UnitTests.Markov;

public sealed class MarkovTransitionResolverTests
{
    [Fact]
    public void ResolveNextState_ShouldReturnFirstState_WhenSampleFallsInFirstInterval()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");
        var matrix = CreateMatrix(calm, wary);

        var resolver = new MarkovTransitionResolver(new DeterministicMarkovSampler());

        var next = resolver.ResolveNextState(matrix, calm, 0.69m);

        next.Should().Be(calm);
    }

    [Fact]
    public void ResolveNextState_ShouldReturnSecondState_WhenSampleFallsInSecondInterval()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");
        var matrix = CreateMatrix(calm, wary);

        var resolver = new MarkovTransitionResolver(new DeterministicMarkovSampler());

        var next = resolver.ResolveNextState(matrix, calm, 0.70m);

        next.Should().Be(wary);
    }

    [Fact]
    public void ResolveNextState_ShouldBeDeterministic_ForSameInputs()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");
        var matrix = CreateMatrix(calm, wary);

        var resolver = new MarkovTransitionResolver(new DeterministicMarkovSampler());

        var first = resolver.ResolveNextState(
            matrix,
            calm,
            seed: "run-seed-001",
            scope: "npc-attitude",
            step: 3);

        var second = resolver.ResolveNextState(
            matrix,
            calm,
            seed: "run-seed-001",
            scope: "npc-attitude",
            step: 3);

        second.Should().Be(first);
    }

    [Fact]
    public void ResolveNextState_ShouldThrow_WhenSampleIsOne()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");
        var matrix = CreateMatrix(calm, wary);

        var resolver = new MarkovTransitionResolver(new DeterministicMarkovSampler());

        var act = () => resolver.ResolveNextState(matrix, calm, 1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static MarkovTransitionMatrix CreateMatrix(
        MarkovState calm,
        MarkovState wary)
    {
        return MarkovTransitionMatrix.Create(
            "npc-attitude-test",
            "markov-0.1.0",
            new[] { calm, wary },
            new[]
            {
                MarkovTransitionRow.Create(
                    calm,
                    new Dictionary<MarkovState, decimal>
                    {
                        [calm] = 0.7m,
                        [wary] = 0.3m
                    }),
                MarkovTransitionRow.Create(
                    wary,
                    new Dictionary<MarkovState, decimal>
                    {
                        [calm] = 0.2m,
                        [wary] = 0.8m
                    })
            });
    }
}