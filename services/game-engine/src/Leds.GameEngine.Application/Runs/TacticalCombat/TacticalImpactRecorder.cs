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
    public sealed record Snapshot(Guid CombatantId, int Vitality, bool WasDefeated, int Guard);

    public static IReadOnlyList<Snapshot> Capture(IEnumerable<Combatant> targets) =>
        [.. targets.Select(t => new Snapshot(t.Id.Value, t.CurrentVitality, t.IsDefeated, t.Guard))];

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
            // The Guard ledger only ever goes down from a hit (it's restored to base at the
            // start of a round, never mid-resolution) — anything it lost this diff is exactly
            // what it kept off the vitality total below.
            var guardAbsorbed = Math.Max(0, snapshot.Guard - target.Guard);

            // Manquée ET intacte : une cible qu'un second effet de la même compétence a
            // touchée a bien perdu de la vitalité, et c'est ce chiffre-là qui doit s'afficher.
            var missed = delta == 0 && guardAbsorbed == 0 && missedIds.Contains(snapshot.CombatantId);

            // Un coup entièrement absorbé laisse la vitalité intacte (delta == 0) mais n'est
            // pas rien : la Garde a fait exactement ce pour quoi elle existe, et guardAbsorbed
            // porte seul la preuve qu'il s'est passé quelque chose.
            if (delta == 0 && !newlyDefeated && !missed && guardAbsorbed == 0)
                continue;

            var position = combat.PositionOf(snapshot.CombatantId);

            impacts.Add(new TacticalImpactDto(
                snapshot.CombatantId, position.X, position.Y, delta, newlyDefeated, missed, guardAbsorbed));
        }

        return impacts;
    }

    /// <summary>
    /// The DoT/HoT ticks a combatant's own activation just resolved (see
    /// <c>TacticalCombat.LastActivationStatusTicks</c>), as its own timeline event — or null
    /// when nothing periodic dealt damage or healing this activation. Called right after every
    /// <c>AdvanceToNextCombatant()</c>, the only place ticks are ever applied.
    /// </summary>
    public static TacticalCombatEventDto? BuildTickEvent(Domain.Combats.Tactical.TacticalCombat combat)
    {
        if (combat.LastActivationCombatantId is not { } combatantId)
            return null;

        var damageOrHealing = combat.LastActivationStatusTicks
            .Where(tick => !tick.Expired
                && tick.Amount > 0
                && tick.Kind is Domain.Combats.StatusEffects.StatusEffectKind.DamageOverTime
                    or Domain.Combats.StatusEffects.StatusEffectKind.HealOverTime)
            .ToArray();
        if (damageOrHealing.Length == 0)
            return null;

        var combatant = combat.Allies.Concat(combat.Enemies)
            .FirstOrDefault(c => c.Id.Value == combatantId);
        if (combatant is null)
            return null;

        var position = combat.PositionOf(combatantId);
        var impacts = damageOrHealing
            .Select(tick => new TacticalImpactDto(
                combatantId,
                position.X,
                position.Y,
                // Same sign convention as every other impact (see TacticalImpactDto: "Vitalité
                // perdue. Négative pour un soin"): damage over time is a POSITIVE vitality
                // delta, healing over time the reverse.
                tick.Kind == Domain.Combats.StatusEffects.StatusEffectKind.DamageOverTime
                    ? tick.Amount
                    : -tick.Amount,
                Defeated: combatant.IsDefeated))
            .ToArray();

        return TacticalCombatEventDto.Tick(combatantId, combatant.DisplayName, position, impacts);
    }
}
