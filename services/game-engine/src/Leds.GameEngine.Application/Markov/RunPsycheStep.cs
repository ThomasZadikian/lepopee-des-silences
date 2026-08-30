using Leds.GameEngine.Domain.Markov.Psyche;

namespace Leds.GameEngine.Application.Markov;

/// <summary>État de la psyché juste après avoir intégré la salle de profondeur <paramref name="Depth"/>.</summary>
public sealed record RunPsycheStep(int Depth, RunPsyche Psyche);