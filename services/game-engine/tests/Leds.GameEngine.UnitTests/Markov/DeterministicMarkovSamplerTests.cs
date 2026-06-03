using FluentAssertions;
using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.UnitTests.Markov;

public sealed class DeterministicMarkovSamplerTests
{
    [Fact]
    public void NextUnitInterval_ShouldReturnValueGreaterThanOrEqualToZeroAndLowerThanOne()
    {
        var sampler = new DeterministicMarkovSampler();

        var value = sampler.NextUnitInterval(
            seed: "run-seed-001",
            scope: "npc-attitude",
            matrixKey: "npc-attitude-test",
            matrixVersion: "markov-0.1.0",
            currentState: "Calm",
            step: 1);

        value.Should().BeGreaterThanOrEqualTo(0m);
        value.Should().BeLessThan(1m);
    }

    [Fact]
    public void NextUnitInterval_ShouldBeDeterministic_ForSameInputs()
    {
        var sampler = new DeterministicMarkovSampler();

        var first = sampler.NextUnitInterval(
            seed: "run-seed-001",
            scope: "npc-attitude",
            matrixKey: "npc-attitude-test",
            matrixVersion: "markov-0.1.0",
            currentState: "Calm",
            step: 1);

        var second = sampler.NextUnitInterval(
            seed: "run-seed-001",
            scope: "npc-attitude",
            matrixKey: "npc-attitude-test",
            matrixVersion: "markov-0.1.0",
            currentState: "Calm",
            step: 1);

        second.Should().Be(first);
    }

    [Fact]
    public void NextUnitInterval_ShouldChange_WhenStepChanges()
    {
        var sampler = new DeterministicMarkovSampler();

        var first = sampler.NextUnitInterval(
            seed: "run-seed-001",
            scope: "npc-attitude",
            matrixKey: "npc-attitude-test",
            matrixVersion: "markov-0.1.0",
            currentState: "Calm",
            step: 1);

        var second = sampler.NextUnitInterval(
            seed: "run-seed-001",
            scope: "npc-attitude",
            matrixKey: "npc-attitude-test",
            matrixVersion: "markov-0.1.0",
            currentState: "Calm",
            step: 2);

        second.Should().NotBe(first);
    }

    [Fact]
    public void NextUnitInterval_ShouldChange_WhenMatrixVersionChanges()
    {
        var sampler = new DeterministicMarkovSampler();

        var first = sampler.NextUnitInterval(
            seed: "run-seed-001",
            scope: "npc-attitude",
            matrixKey: "npc-attitude-test",
            matrixVersion: "markov-0.1.0",
            currentState: "Calm",
            step: 1);

        var second = sampler.NextUnitInterval(
            seed: "run-seed-001",
            scope: "npc-attitude",
            matrixKey: "npc-attitude-test",
            matrixVersion: "markov-0.2.0",
            currentState: "Calm",
            step: 1);

        second.Should().NotBe(first);
    }

    [Fact]
    public void NextUnitInterval_ShouldThrow_WhenStepIsNegative()
    {
        var sampler = new DeterministicMarkovSampler();

        var act = () => sampler.NextUnitInterval(
            seed: "run-seed-001",
            scope: "npc-attitude",
            matrixKey: "npc-attitude-test",
            matrixVersion: "markov-0.1.0",
            currentState: "Calm",
            step: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}