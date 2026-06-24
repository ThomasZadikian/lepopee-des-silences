using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats.Atb;

/// <summary>
/// Prepares a freshly created combat for the ATB clock: bakes each combatant's
/// Markov tempo (fill rate) and opening gauge from the run's psyche, then advances
/// once to elect the opener. Called only at combat creation — fill rates persist,
/// the psyche is stable within a single fight.
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
                Speed: combatant.BaseStatSnapshot.Speed,
                Initiative: combatant.BaseStatSnapshot.Initiative,
                Side: combatant.Side,
                CombatantKey: combatant.SourceKey,
                Psyche: psyche,
                Seed: run.Seed,
                Tick: combat.CurrentTick));

            combatant.SetAtbFillPerTick(tempo.FillPerTick);
            combatant.SetAtbGauge(tempo.OpeningGauge);
        }

        combat.AdvanceTurn();
    }
}