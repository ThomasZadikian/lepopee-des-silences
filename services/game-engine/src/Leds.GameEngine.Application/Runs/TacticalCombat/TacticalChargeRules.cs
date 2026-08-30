using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

public static class TacticalChargeRules
{
    public static void AwardUsefulAction(
        Combatant actor,
        IReadOnlyCollection<Combatant> targets,
        IReadOnlyCollection<TacticalImpactDto> impacts)
    {
        var useful = impacts.Where(i => i.VitalityDelta != 0).ToArray();
        if (useful.Length == 0)
            return;

        var actorGain = 0.3m + (0.1m * Math.Max(0, useful.Length - 1));
        actorGain += 0.3m * useful.Count(i => i.Defeated);
        actor.GainCharge(Math.Min(2m, actorGain));

        foreach (var impact in useful.Where(i => i.VitalityDelta > 0))
        {
            var target = targets.FirstOrDefault(t => t.Id.Value == impact.CombatantId);
            if (target is not null && !target.IsDefeated && target.Id != actor.Id)
                target.GainCharge(0.3m);
        }
    }
}
