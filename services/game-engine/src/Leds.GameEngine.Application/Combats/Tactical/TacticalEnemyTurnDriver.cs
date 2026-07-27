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
            Message: $"« {strike.DisplayName} » lancé par {actor.DisplayName}.",
            ActorId: actorId,
            SkillKey: strike.Key,
            TargetIds: [prey.Id.Value]));

        log.AddRange(resolution.LogEntries);

        return log;
    }

    /// <summary>
    /// La compétence offensive la plus puissante qui atteigne réellement la proie depuis la case
    /// où l'ennemi vient de se poster, ou <c>null</c> s'il est encore trop loin.
    /// </summary>
    private static CombatantSkill? ChooseReachableSkill(
        Domain.Combats.Tactical.TacticalCombat combat, Combatant actor, Combatant prey)
    {
        var origin = combat.PositionOf(actor.Id.Value);
        var target = combat.PositionOf(prey.Id.Value);

        return actor.Skills
            .Where(s => TacticalTargeting.IsHostile(s.TargetingType))
            .Where(s =>
            {
                var (range, needsSight) = TacticalRange.For(s);
                return TacticalTargeting.IsInRange(
                    combat.Battlefield, origin, target, range, needsSight);
            })
            .OrderByDescending(s => s.BasePower)
            // Départage stable : sans lui, deux compétences de même puissance dépendraient de
            // l'ordre d'énumération et le même combat rejoué divergerait.
            .ThenBy(s => s.Key, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
