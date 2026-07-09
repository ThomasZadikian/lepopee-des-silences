using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Markov.Psyche;

namespace Leds.GameEngine.Application.Combats.Atb;

public sealed record AtbTempoContext(
    int Initiative,
    CombatantSide Side,
    string CombatantKey,
    RunPsyche Psyche,
    string Seed,
    long Tick);

/// <summary>
/// Markov room/side tempo factors (per-mille, baked once at combat prep) plus
/// the opening gauge. The final fill rate is computed live from these factors
/// combined with EFFECTIVE stats — see <see cref="Leds.GameEngine.Domain.Combats.Atb.AtbTempoFormula"/>.
/// </summary>
public sealed record AtbTempoResult(int RoomFactorPerMille, int CombatantFactorPerMille, int OpeningGauge);

/// <summary>
/// Resolves the Markov-driven ATB tempo factors (room/side factors + opening
/// gauge) for a combatant from the run's psyche.
/// </summary>
public interface IAtbTempoProvider
{
    AtbTempoResult Resolve(AtbTempoContext context);
}