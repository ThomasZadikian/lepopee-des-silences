using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Infrastructure.Generation.Psyche;

public sealed class RunPsycheEvolver : IRunPsycheEvolver
{
    private readonly EmotionalCalibration _calibration;

    public RunPsycheEvolver(EmotionalCalibration calibration) => _calibration = calibration;

    public RunPsyche Evolve(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var tendency = _calibration.BuildTendencyMatrix();
        var psyche = RunPsyche.Initial();

        // Repli ordonné : chaque salle déjà traversée fait évoluer puis pousser la psyché.
        foreach (var room in run.Rooms.OrderBy(r => r.Depth))
        {
            var advanced = tendency.Advance(psyche.Distribution); // π(t+1)=π(t)×P  (auto-influence)
            var nudged = _calibration.Nudge(advanced, room);      // accumulation de la tonalité
            psyche = RunPsyche.From(nudged);
        }

        return psyche;
    }
}