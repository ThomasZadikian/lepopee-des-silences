using FluentAssertions;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Psyche;

namespace Leds.GameEngine.UnitTests.Markov.Psyche;

public sealed class RunPsycheEvolutionTests
{
    private readonly EmotionalCalibration _calibration = new();

    private RunPsyche FoldPainful(int steps)
    {
        var tendency = _calibration.BuildTendencyMatrix();
        var psyche = RunPsyche.Initial(); // Calm
        for (var i = 0; i < steps; i++)
        {
            var advanced = tendency.Advance(psyche.Distribution);      // π(t+1)=π(t)×P
            var nudged = _calibration.Nudge(advanced, PalaceRoomState.Painful);
            psyche = RunPsyche.From(nudged);
        }
        return psyche;
    }

    [Fact]
    public void Repeated_painful_rooms_shift_dominant_emotion()
    {
        // Une run qui s'enfonce dans le douloureux dérive durablement (≠ amnésique).
        FoldPainful(8).Dominant()
            .Should().BeOneOf(EmotionalState.Fragmented, EmotionalState.Dissociated);
    }

    [Fact]
    public void Evolution_is_deterministic()
    {
        var a = FoldPainful(8);
        var b = FoldPainful(8);

        foreach (var state in RunPsyche.States)
            a.ProbabilityOf(state).Should().Be(b.ProbabilityOf(state));
    }

    [Fact]
    public void Initial_psyche_is_a_dirac_on_baseline()
    {
        RunPsyche.Initial(EmotionalState.Calm).ProbabilityOf(EmotionalState.Calm).Should().Be(1m);
    }

    [Fact]
    public void Different_resolved_choices_push_the_psyche_differently()
    {
        var calibration = new EmotionalCalibration();
        var start = RunPsyche.Initial().Distribution;

        var afterCombat = calibration.Nudge(
            start, new RoomPsycheSignal(PalaceRoomState.Neutral, ResolvedChoices: new[] { NodeEventType.Combat }));
        var afterRest = calibration.Nudge(
            start, new RoomPsycheSignal(PalaceRoomState.Neutral, ResolvedChoices: new[] { NodeEventType.Rest }));

        var wary = MarkovState.Create(EmotionalState.Wary.ToString());
        // Combattre tend vers Wary ; se reposer vers Calm → le choix change bien la psyché.
        afterCombat.Probabilities[wary].Should().BeGreaterThan(afterRest.Probabilities[wary]);
    }
}