using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Markov;

public interface IRunPsycheEvolver
{
    /// <summary>Calcule la psyché courante par repli déterministe sur l'historique de salles.</summary>
    RunPsyche Evolve(Run run);
}