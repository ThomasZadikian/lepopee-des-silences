using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats.Atb;

/// <summary>
/// Prepares a freshly created combat for the ATB clock: bakes each combatant's
/// Markov room/side tempo factors and opening gauge from the run's psyche (stable
/// for the whole fight), then elects the opener. The actual fill rate is derived
/// live from these factors plus effective stats — see <see cref="Combat.RecalculateAllTempo"/> —
/// and is recomputed on every subsequent clock advance, not just at creation.
/// </summary>
public interface IAtbCombatPreparer
{
    void PrepareNewCombat(Combat combat, Run run);
}

public sealed class AtbCombatPreparer : IAtbCombatPreparer
{
    private readonly IAtbTempoProvider _tempoProvider;
    private readonly IRunPsycheEvolver _psycheEvolver;

    public AtbCombatPreparer(IAtbTempoProvider tempoProvider, IRunPsycheEvolver psycheEvolver)
    {
        _tempoProvider = tempoProvider;
        _psycheEvolver = psycheEvolver;
    }

    public void PrepareNewCombat(Combat combat, Run run)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(run);

        var psyche = _psycheEvolver.Evolve(run);

        foreach (var combatant in combat.Allies.Concat(combat.Enemies))
        {
            var tempo = _tempoProvider.Resolve(new AtbTempoContext(
                Initiative: combatant.BaseStatSnapshot.Initiative,
                Side: combatant.Side,
                CombatantKey: combatant.SourceKey,
                Psyche: psyche,
                Seed: run.Seed,
                Tick: combat.CurrentTick));

            combatant.SetAtbTempoFactors(tempo.RoomFactorPerMille, tempo.CombatantFactorPerMille);
            combatant.SetAtbGauge(tempo.OpeningGauge);
        }

        // Bake the first live fill rates (effective stats + opposing side aggregate)
        // now that room/side factors and opening gauges are set.
        combat.RecalculateAllTempo();

        // Elect an opener only if someone is already ready; otherwise combat opens
        // with partial bars and the real-time clock fills them from the opening
        // spread (no fast-forward — time only ever moves through the hold clock).
        combat.ElectActiveByReadiness();
    }
}