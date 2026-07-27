using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

/// <summary>
/// Mesure ce qu'une compétence a réellement fait, par différence d'état.
/// </summary>
/// <remarks>
/// Le noyau de résolution est partagé avec l'ATB et ne rend pas de compte chiffré : il applique
/// dégâts, soins, statuts et Lois, puis renvoie des lignes de journal. Plutôt que de le forcer à
/// tenir une comptabilité dont l'ATB n'a pas besoin, on relève la vitalité avant et après —
/// c'est le même procédé que <c>CombatMetricsCalculator</c> côté ATB, et il capte du même coup
/// tout ce qu'une Loi aurait ajouté en chemin.
/// </remarks>
public static class TacticalImpactRecorder
{
    public sealed record Snapshot(Guid CombatantId, int Vitality, bool WasDefeated);

    public static IReadOnlyList<Snapshot> Capture(IEnumerable<Combatant> targets) =>
        [.. targets.Select(t => new Snapshot(t.Id.Value, t.CurrentVitality, t.IsDefeated))];

    /// <summary>
    /// Compare l'état relevé à l'état courant et produit un impact par cible réellement touchée.
    /// Une cible inchangée n'en produit aucun : un chiffre « 0 » qui s'envole au-dessus d'une
    /// tête raconterait un coup qui n'a pas eu lieu.
    /// </summary>
    public static IReadOnlyList<TacticalImpactDto> Diff(
        IReadOnlyList<Snapshot> before,
        IReadOnlyCollection<Combatant> targets,
        Domain.Combats.Tactical.TacticalCombat combat)
    {
        var impacts = new List<TacticalImpactDto>();

        foreach (var snapshot in before)
        {
            var target = targets.FirstOrDefault(t => t.Id.Value == snapshot.CombatantId);
            if (target is null)
                continue;

            var delta = snapshot.Vitality - target.CurrentVitality;
            var newlyDefeated = target.IsDefeated && !snapshot.WasDefeated;

            if (delta == 0 && !newlyDefeated)
                continue;

            var position = combat.PositionOf(snapshot.CombatantId);

            impacts.Add(new TacticalImpactDto(
                snapshot.CombatantId, position.X, position.Y, delta, newlyDefeated));
        }

        return impacts;
    }
}
