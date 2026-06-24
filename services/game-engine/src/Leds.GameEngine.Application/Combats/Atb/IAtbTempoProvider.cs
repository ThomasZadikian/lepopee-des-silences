using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Markov.Psyche;

namespace Leds.GameEngine.Application.Combats.Atb;

public sealed record AtbTempoContext(
    int Speed,
    int Initiative,
    CombatantSide Side,
    string CombatantKey,
    RunPsyche Psyche,
    string Seed,
    long Tick);

public sealed record AtbTempoResult(int FillPerTick, int OpeningGauge);

/// <summary>
/// Resolves the Markov-driven ATB tempo (fill rate + opening gauge) for a
/// combatant from the run's psyche.
/// </summary>
public interface IAtbTempoProvider
{
    AtbTempoResult Resolve(AtbTempoContext context);
}