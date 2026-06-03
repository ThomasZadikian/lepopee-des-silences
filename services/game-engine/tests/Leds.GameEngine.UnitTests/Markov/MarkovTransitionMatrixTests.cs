using FluentAssertions;
using Leds.GameEngine.Domain.Markov;

namespace Leds.GameEngine.UnitTests.Markov;

public sealed class MarkovTransitionMatrixTests
{
    [Fact]
    public void Create_ShouldThrow_WhenKeyIsBlank()
    {
        var calm = MarkovState.Create("Calm");

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                calm,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 1m
                })
        };

        var act = () => MarkovTransitionMatrix.Create(
            " ",
            "markov-0.1.0",
            new[] { calm },
            rows);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Markov matrix key is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenVersionIsBlank()
    {
        var calm = MarkovState.Create("Calm");

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                calm,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 1m
                })
        };

        var act = () => MarkovTransitionMatrix.Create(
            "npc-attitude-test",
            " ",
            new[] { calm },
            rows);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Markov matrix version is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenStatesAreEmpty()
    {
        var act = () => MarkovTransitionMatrix.Create(
            "test-matrix",
            "markov-0.1.0",
            Array.Empty<MarkovState>(),
            Array.Empty<MarkovTransitionRow>());

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Markov matrix must contain at least one state.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDuplicateStatesExist()
    {
        var calm = MarkovState.Create("Calm");
        var calmDuplicate = MarkovState.Create("calm");

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                calm,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 1m
                })
        };

        var act = () => MarkovTransitionMatrix.Create(
            "npc-attitude-test",
            "markov-0.1.0",
            new[] { calm, calmDuplicate },
            rows);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Duplicate Markov state detected: 'Calm'.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenRowIsMissing()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                calm,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 0.7m,
                    [wary] = 0.3m
                })
        };

        var act = () => MarkovTransitionMatrix.Create(
            "npc-attitude-test",
            "markov-0.1.0",
            new[] { calm, wary },
            rows);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Missing transition row for state 'Wary'.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDuplicateRowsExist()
    {
        var calm = MarkovState.Create("Calm");

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                calm,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 1m
                }),
            MarkovTransitionRow.Create(
                MarkovState.Create("calm"),
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 1m
                })
        };

        var act = () => MarkovTransitionMatrix.Create(
            "npc-attitude-test",
            "markov-0.1.0",
            new[] { calm },
            rows);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Duplicate transition row detected for state 'Calm'.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenTargetStateIsMissing()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                calm,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 1m
                }),
            MarkovTransitionRow.Create(
                wary,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 0.2m,
                    [wary] = 0.8m
                })
        };

        var act = () => MarkovTransitionMatrix.Create(
            "npc-attitude-test",
            "markov-0.1.0",
            new[] { calm, wary },
            rows);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Transition row for state 'Calm' is missing target state 'Wary'.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenProbabilityIsNegative()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                calm,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = -0.1m,
                    [wary] = 1.1m
                }),
            MarkovTransitionRow.Create(
                wary,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 0.2m,
                    [wary] = 0.8m
                })
        };

        var act = () => MarkovTransitionMatrix.Create(
            "npc-attitude-test",
            "markov-0.1.0",
            new[] { calm, wary },
            rows);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Transition probability from 'Calm' to 'Calm' must be between 0 and 1.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenRowDoesNotSumToOne()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");

        var rows = new[]
        {
            MarkovTransitionRow.Create(
                calm,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 0.6m,
                    [wary] = 0.3m
                }),
            MarkovTransitionRow.Create(
                wary,
                new Dictionary<MarkovState, decimal>
                {
                    [calm] = 0.2m,
                    [wary] = 0.8m
                })
        };

        var act = () => MarkovTransitionMatrix.Create(
            "npc-attitude-test",
            "markov-0.1.0",
            new[] { calm, wary },
            rows);

        act.Should().Throw<MarkovMatrixValidationException>()
            .WithMessage("Transition probabilities for state 'Calm' must sum to 1. Actual sum is 0.9.");
    }

    [Fact]
    public void Advance_ShouldApplyMatrixMultiplicationToDistribution()
    {
        var calm = MarkovState.Create("Calm");
        var wary = MarkovState.Create("Wary");

        var matrix = CreateTwoStateMatrix(calm, wary);

        var distribution = MarkovStateDistribution.Create(
            new Dictionary<MarkovState, decimal>
            {
                [calm] = 0.5m,
                [wary] = 0.5m
            });

        var next = matrix.Advance(distribution);

        next[calm].Should().Be(0.45m);
        next[wary].Should().Be(0.55m);
    }

    private static MarkovTransitionMatrix CreateTwoStateMatrix(
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