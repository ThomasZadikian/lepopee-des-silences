using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Markov;

public interface IRunPsycheEvolver
{
    RunPsyche Evolve(Run run);

    /// <summary>Trajectoire complète de la psyché (un point par salle traversée).</summary>
    IReadOnlyList<RunPsycheStep> EvolveTrajectory(Run run);
}