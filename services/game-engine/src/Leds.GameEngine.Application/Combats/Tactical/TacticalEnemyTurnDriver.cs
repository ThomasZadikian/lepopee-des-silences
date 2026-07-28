using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.SharedBuildingBlocks.Time;

namespace Leds.GameEngine.Application.Combats.Tactical;

/// <summary>Ce qu'un enchaînement de tours ennemis a produit.</summary>
public sealed record TacticalEnemyTurnsResult(
    IReadOnlyList<CombatLogEntryDto> LogEntries,
    IReadOnlyList<TacticalCombatEventDto> Events);

public interface ITacticalEnemyTurnDriver
{
    /// <summary>
    /// Joue les tours ennemis tant que la main leur appartient, et s'arrête dès qu'un allié la
    /// reprend ou que le combat s'achève.
    /// </summary>
    TacticalEnemyTurnsResult PlayWhileEnemyHasInitiative(Domain.Combats.Tactical.TacticalCombat combat);
}

/// <summary>
/// Fait agir les ennemis.
/// </summary>
/// <remarks>
/// <para>
/// Extrait de la commande de fin de tour parce qu'il faut le même enchaînement à <b>deux</b>
/// moments : quand le joueur passe la main, et dès l'ouverture du combat si l'initiative revient
/// à l'adversaire. Sans le second, un combat où la créature la plus rapide ouvre restait bloqué
/// pour toujours — le joueur n'a pas la main, et rien ne la lui rend.
/// </para>
/// </remarks>
public sealed class TacticalEnemyTurnDriver : ITacticalEnemyTurnDriver
{
    /// <summary>
    /// Plafond de sécurité sur les tours enchaînés. Un combat réel n'en produit qu'une poignée ;
    /// ce garde-fou n'existe que pour qu'un état incohérent échoue franchement au lieu de
    /// boucler indéfiniment dans une requête HTTP.
    /// </summary>
    private const int MaxChainedEnemyTurns = 64;

    /// <summary>Seuil de PV pour déclencher un soin (50%).</summary>
    private const double HealThreshold = 0.5;

    /// <summary>Seuil de garde pour déclencher un buff (30% de la garde max).</summary>
    private const double GuardThreshold = 0.3;

    private readonly ICombatSkillEffectResolver _effectResolver;
    private readonly IClock _clock;

    public TacticalEnemyTurnDriver(ICombatSkillEffectResolver effectResolver, IClock clock)
    {
        _effectResolver = effectResolver;
        _clock = clock;
    }

    public TacticalEnemyTurnsResult PlayWhileEnemyHasInitiative(
        Domain.Combats.Tactical.TacticalCombat combat)
    {
        ArgumentNullException.ThrowIfNull(combat);

        var log = new List<CombatLogEntryDto>();
        var events = new List<TacticalCombatEventDto>();
        var guard = 0;

        while (combat.Status == CombatStatus.Active && IsEnemyTurn(combat))
        {
            if (++guard > MaxChainedEnemyTurns)
                throw new InvalidOperationException(
                    "Tactical enemy turns did not settle; aborting to avoid an endless loop.");

            log.AddRange(PlayOneTurn(combat, events));

            combat.CompleteIfAllEnemiesDefeated();
            combat.FailIfAllAlliesDefeated();

            if (combat.Status != CombatStatus.Active)
                break;

            combat.AdvanceToNextCombatant();
        }

        return new TacticalEnemyTurnsResult(log, events);
    }

    private static bool IsEnemyTurn(Domain.Combats.Tactical.TacticalCombat combat) =>
        combat.ActiveCombatantId is { } id && combat.Enemies.Any(e => e.Id.Value == id);

    /// <summary>
    /// Un tour ennemi : choisir une proie, marcher vers elle, frapper si elle est à portée.
    /// </summary>
    /// <remarks>
    /// L'ordre — cible, puis chemin — vient de <see cref="TacticalEnemyAi"/> : une créature hors
    /// de portée avance quand même plutôt que de gâcher son tour sur place.
    /// </remarks>
    private IReadOnlyCollection<CombatLogEntryDto> PlayOneTurn(
        Domain.Combats.Tactical.TacticalCombat combat,
        List<TacticalCombatEventDto> events)
    {
        var log = new List<CombatLogEntryDto>();

        var actorId = combat.ActiveCombatantId!.Value;
        var actor = combat.Enemies.First(e => e.Id.Value == actorId);

        var prey = TacticalEnemyAi.ChooseTarget(combat, actorId);
        if (prey is null)
            return log;

        var destination = TacticalEnemyAi.ChooseDestination(combat, actorId, prey);

        if (destination != combat.PositionOf(actorId))
        {
            var move = combat.MoveActiveCombatant(destination);
            events.Add(TacticalCombatEventDto.Move(actorId, actor.DisplayName, move.Path));

            log.Add(new CombatLogEntryDto(
                OccurredAtUtc: _clock.UtcNow.UtcDateTime,
                Type: "TacticalMove",
                Message: $"{actor.DisplayName} avance en ({destination.X}, {destination.Y}) "
                         + $"pour {move.Cost} de mouvement.",
                ActorId: actorId,
                SkillKey: null,
                TargetIds: []));
        }

        var strike = ChooseReachableSkill(combat, actor, prey);
        if (strike is null)
            return log;

        var targets = new[] { prey };

        var before = Runs.TacticalCombat.TacticalImpactRecorder.Capture(targets);
        var resolution = _effectResolver.Resolve(combat, actor, strike, targets);
        var impacts = Runs.TacticalCombat.TacticalImpactRecorder.Diff(before, targets, combat);

        combat.MarkActiveCombatantActed();

        events.Add(TacticalCombatEventDto.Skill(
            actorId, actor.DisplayName, strike.Key, strike.DisplayName, impacts));

        log.Add(new CombatLogEntryDto(
            OccurredAtUtc: _clock.UtcNow.UtcDateTime,
            Type: "ActionAccepted",
            Message: $"\u00ab {strike.DisplayName} \u00bb lancé par {actor.DisplayName}.",
            ActorId: actorId,
            SkillKey: strike.Key,
            TargetIds: [prey.Id.Value]));

        log.AddRange(resolution.LogEntries);

        return log;
    }

    /// <summary>
    /// La compétence offensive ou utilitaire la plus adaptée que l'ennemi peut utiliser.
    /// Priorise les buffs/débuffs si nécessaire (O-009), et évite les AoE alliées (O-007).
    /// </summary>
    private CombatantSkill? ChooseReachableSkill(
        Domain.Combats.Tactical.TacticalCombat combat, Combatant actor, Combatant prey)
    {
        var origin = combat.PositionOf(actor.Id.Value);
        var target = combat.PositionOf(prey.Id.Value);

        // O-009: Séparer les compétences en offensives et utilitaires
        var offensiveSkills = actor.Skills
            .Where(s => TacticalTargeting.IsHostile(s.TargetingType))
            .Where(s =>
            {
                var (range, needsSight) = TacticalRange.For(s);
                return TacticalTargeting.IsInRange(combat.Battlefield, origin, target, range, needsSight);
            })
            .ToList();

        // O-009: Considérer les compétences utilitaires (buffs/débuffs)
        var bestUtilitySkill = ChooseBestUtilitySkill(combat, actor, offensiveSkills.Count > 0);
        if (bestUtilitySkill != null)
            return bestUtilitySkill;

        // O-007: Éviter les AoE qui touchent des alliés
        var safeOffensiveSkills = offensiveSkills
            .Where(s =>
            {
                var shape = TacticalTargeting.ShapeForCatalogTargeting(s.TargetingType);
                // Les compétences Single/Cross ne risquent pas de toucher des alliés
                if (shape == TacticalAreaShape.Single || shape == TacticalAreaShape.Cross)
                    return true;

                // Pour les AoE (Diamond, Map), vérifier si des alliés sont dans la zone
                var affectedCells = TacticalTargeting.CellsInArea(combat.Battlefield, target, shape);
                var targets = TacticalTargeting.ResolveTargets(
                    combat, affectedCells, actor.Side, true, shape);

                // Si des alliés sont dans la zone, exclure cette compétence (O-007)
                return !targets.Any(t => t.Side == actor.Side);
            })
            .ToList();

        // Sinon, utiliser une compétence offensive sûre
        return safeOffensiveSkills
            .OrderByDescending(s => s.BasePower)
            .ThenBy(s => s.Key, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>
    /// Trouve la meilleure cible pour une compétence utilitaire (buff/débuff).
    /// </summary>
    private static GridPosition FindBestAllyTarget(
        TacticalCombat combat, Combatant actor, CombatantSkill skill)
    {
        if (skill.TargetingType == "Self")
            return combat.PositionOf(actor.Id.Value);

        // Pour SingleAlly/AllAllies, cibler l'allié le plus blessé ou le plus proche
        return combat.Enemies
            .Where(e => !e.IsDefeated && e.Id.Value != actor.Id.Value)
            .OrderBy(e => e.CurrentVitality) // Priorité aux alliés les plus blessés
            .Select(e => combat.PositionOf(e.Id.Value))
            .FirstOrDefault();
    }

    /// <summary>
    /// Choisit la meilleure compétence utilitaire en fonction du contexte (O-009).
    /// </summary>
    private CombatantSkill? ChooseBestUtilitySkill(
        TacticalCombat combat, Combatant actor, bool hasOffensiveSkills)
    {
        // Si pas de compétences offensives, toujours utiliser un utilitaire
        if (!hasOffensiveSkills)
        {
            return actor.Skills
                .Where(s => !TacticalTargeting.IsHostile(s.TargetingType))
                .OrderByDescending(s => s.BasePower) // Priorité aux buffs les plus puissants
                .ThenBy(s => s.Key, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        // Vérifier si un allié a besoin d'un soin
        var woundedAlly = combat.Enemies
            .Where(e => !e.IsDefeated && e.Id.Value != actor.Id.Value)
            .FirstOrDefault(e => e.CurrentVitality <= e.MaxVitality * HealThreshold);

        if (woundedAlly != null)
        {
            var healSkill = actor.Skills
                .FirstOrDefault(s =>
                    s.SkillType == "Heal" &&
                    TacticalTargeting.IsInRange(
                        combat.Battlefield,
                        combat.PositionOf(actor.Id.Value),
                        combat.PositionOf(woundedAlly.Id.Value),
                        TacticalRange.For(s).range,
                        TacticalRange.For(s).needsSight));
            if (healSkill != null)
                return healSkill;
        }

        // Vérifier si un allié a besoin de garde
        var lowGuardAlly = combat.Enemies
            .Where(e => !e.IsDefeated && e.Id.Value != actor.Id.Value)
            .FirstOrDefault(e => e.Guard <= e.MaxVitality * GuardThreshold);

        if (lowGuardAlly != null)
        {
            var guardSkill = actor.Skills
                .FirstOrDefault(s =>
                    s.SkillType == "Guard" &&
                    TacticalTargeting.IsInRange(
                        combat.Battlefield,
                        combat.PositionOf(actor.Id.Value),
                        combat.PositionOf(lowGuardAlly.Id.Value),
                        TacticalRange.For(s).range,
                        TacticalRange.For(s).needsSight));
            if (guardSkill != null)
                return guardSkill;
        }

        // Sinon, utiliser un débuff si possible
        var debuffSkill = actor.Skills
            .FirstOrDefault(s => s.SkillType == "Debuff" || s.SkillType == "Status");
        if (debuffSkill != null)
        {
            // Vérifier que la cible est en portée
            var (range, needsSight) = TacticalRange.For(debuffSkill);
            var targetPos = combat.PositionOf(prey.Id.Value);
            if (TacticalTargeting.IsInRange(combat.Battlefield, combat.PositionOf(actor.Id.Value), targetPos, range, needsSight))
                return debuffSkill;
        }

        return null; // Pas de compétence utilitaire nécessaire
    }
}
