using Leds.GameEngine.Application.Combats.Actions;
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

    /// <summary>Type de ligne de journal émis par le noyau de résolution sur un jet raté.</summary>
    private const string MissLogType = "AttackMissed";

    /// <summary>
    /// Compare l'état relevé à l'état courant et produit un impact par cible réellement touchée.
    /// Une cible inchangée n'en produit aucun : un chiffre « 0 » qui s'envole au-dessus d'une
    /// tête raconterait un coup qui n'a pas eu lieu.
    /// </summary>
    /// <param name="logEntries">
    /// Le journal rendu par la résolution. Il porte la seule trace d'une esquive : la vitalité
    /// d'une cible manquée est rigoureusement identique avant et après, si bien que la
    /// différence d'état seule ne peut pas distinguer « manqué » de « pas visé ». Sans ce
    /// recoupement, une compétence entièrement ratée part sans laisser le moindre signe à
    /// l'écran, et le joueur ne sait pas pourquoi rien ne s'est produit.
    /// </param>
    public static IReadOnlyList<TacticalImpactDto> Diff(
        IReadOnlyList<Snapshot> before,
        IReadOnlyCollection<Combatant> targets,
        Domain.Combats.Tactical.TacticalCombat combat,
        IReadOnlyCollection<CombatLogEntryDto>? logEntries = null)
    {
        var missedIds = logEntries is null
            ? []
            : logEntries
                .Where(e => string.Equals(e.Type, MissLogType, StringComparison.Ordinal))
                .SelectMany(e => e.TargetIds)
                .ToHashSet();

        var impacts = new List<TacticalImpactDto>();

        foreach (var snapshot in before)
        {
            var target = targets.FirstOrDefault(t => t.Id.Value == snapshot.CombatantId);
            if (target is null)
                continue;

            var delta = snapshot.Vitality - target.CurrentVitality;
            var newlyDefeated = target.IsDefeated && !snapshot.WasDefeated;

            // Manquée ET intacte : une cible qu'un second effet de la même compétence a
            // touchée a bien perdu de la vitalité, et c'est ce chiffre-là qui doit s'afficher.
            var missed = delta == 0 && missedIds.Contains(snapshot.CombatantId);

            if (delta == 0 && !newlyDefeated && !missed)
                continue;

            var position = combat.PositionOf(snapshot.CombatantId);

            impacts.Add(new TacticalImpactDto(
                snapshot.CombatantId, position.X, position.Y, delta, newlyDefeated, missed));
        }

        return impacts;
    }
}
