using FluentAssertions;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Infrastructure.Generation.Psyche;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

namespace Leds.GameEngine.UnitTests.Markov.Psyche;

public sealed class EmotionalBiasTests
{
    private const decimal Tolerance = 0.000001m;

    private static MarkovTransitionMatrix BaseMatrix() =>
        new StaticRoomTypeMarkovMatrixProvider()
            .GetMatrix(StaticRoomTypeMarkovMatrixProvider.SupportedVersion);

    [Fact]
    public void Biased_matrix_rows_still_sum_to_one()
    {
        var pref = new EmotionalCalibration()
            .ProjectToRoomTypes(RunPsyche.Initial(EmotionalState.Fragmented));

        var biased = EmotionalBias.ApplyPreference(BaseMatrix(), pref, 0.35m);

        foreach (var state in biased.States)
            biased[state].Probabilities.Values.Sum()
                .Should().BeInRange(1m - Tolerance, 1m + Tolerance);
    }

    [Fact]
    public void Alpha_zero_returns_the_base_matrix_unchanged()
    {
        var baseMatrix = BaseMatrix();
        var pref = new EmotionalCalibration()
            .ProjectToRoomTypes(RunPsyche.Initial(EmotionalState.Fragmented));

        EmotionalBias.ApplyPreference(baseMatrix, pref, 0m).Should().BeSameAs(baseMatrix);
    }
}