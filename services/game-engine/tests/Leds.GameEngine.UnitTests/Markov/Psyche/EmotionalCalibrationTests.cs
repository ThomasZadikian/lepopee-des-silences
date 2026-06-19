using FluentAssertions;
using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Psyche;

namespace Leds.GameEngine.UnitTests.Markov.Psyche;

public sealed class EmotionalCalibrationTests
{
    private const decimal Tolerance = 0.000001m;
    private readonly EmotionalCalibration _calibration = new();

    [Fact]
    public void TendencyMatrix_is_a_valid_markov_matrix()
    {
        // Lève MarkovMatrixValidationException si une ligne À CALIBRER ne somme pas à 1.
        var act = () => _calibration.BuildTendencyMatrix();
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(EmotionalState.Calm)]
    [InlineData(EmotionalState.Wary)]
    [InlineData(EmotionalState.Withdrawn)]
    [InlineData(EmotionalState.Dissociated)]
    [InlineData(EmotionalState.Fragmented)]
    [InlineData(EmotionalState.Lucid)]
    public void Projections_sum_to_one_for_any_pure_state(EmotionalState state)
    {
        var psyche = RunPsyche.Initial(state);

        _calibration.ProjectToRoomTypes(psyche).Values.Sum()
            .Should().BeInRange(1m - Tolerance, 1m + Tolerance);
        _calibration.ProjectToPalaceStates(psyche).Values.Sum()
            .Should().BeInRange(1m - Tolerance, 1m + Tolerance);
    }

    [Theory]
    [InlineData(PalaceRoomState.Neutral)]
    [InlineData(PalaceRoomState.Silent)]
    [InlineData(PalaceRoomState.Painful)]
    public void Nudge_keeps_a_valid_distribution(PalaceRoomState roomState)
    {
        var nudged = _calibration.Nudge(RunPsyche.Initial().Distribution, roomState);
        nudged.Probabilities.Values.Sum().Should().BeInRange(1m - Tolerance, 1m + Tolerance);
    }
}