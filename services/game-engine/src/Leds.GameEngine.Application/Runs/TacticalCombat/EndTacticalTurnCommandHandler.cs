using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

/// <summary>
/// Clôt le tour du combattant actif, puis joue les tours ennemis jusqu'à ce qu'un allié
/// reprenne la main.
/// </summary>
/// <remarks>
/// <para>
/// Contrairement à l'ATB — où les tours ennemis sont pilotés en temps réel par le client via
/// <c>AdvanceCombatTurnCommand</c> — le tactique les résout <b>en une fois, côté serveur</b>.
/// Le tour par tour n'a pas d'horloge à faire couler : laisser le client redemander tour après
/// tour n'apporterait qu'un aller-retour réseau par ennemi, sans rien changer au résultat. Le
/// journal renvoyé contient de quoi rejouer la séquence à l'écran.
/// </para>
/// </remarks>
public sealed class EndTacticalTurnCommandHandler
    : IRequestHandler<EndTacticalTurnCommand, TacticalCombatResponse>
{
    /// <summary>
    /// Plafond de sécurité sur les tours ennemis enchaînés. Un combat réel n'en produit qu'une
    /// poignée (5 ennemis au plus) ; ce garde-fou n'existe que pour qu'un état incohérent
    /// échoue franchement au lieu de boucler indéfiniment dans une requête HTTP.
    /// </summary>
    private const int MaxChainedEnemyTurns = 64;

    private readonly IRunRepository _runRepository;
    private readonly ICombatSkillEffectResolver _effectResolver;
    private readonly IClock _clock;

    public EndTacticalTurnCommandHandler(
        IRunRepository runRepository,
        ICombatSkillEffectResolver effectResolver,
        IClock clock)
    {
        _runRepository = runRepository;
        _effectResolver = effectResolver;
        _clock = clock;
    }

    public async Task<TacticalCombatResponse> Handle(
        EndTacticalTurnCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var combat = run.RequireActiveTacticalCombat();
        var log = new List<CombatLogEntryDto>();

        combat.AdvanceToNextCombatant();

        var guard = 0;

        while (combat.Status == CombatStatus.Active && IsEnemyTurn(combat))
        {
            if (++guard > MaxChainedEnemyTurns)
                throw new InvalidOperationException(
                    "Tactical enemy turns did not settle; aborting to avoid an endless loop.");

            log.AddRange(PlayEnemyTurn(combat));

            combat.CompleteIfAllEnemiesDefeated();
            combat.FailIfAllAlliesDefeated();

            if (combat.Status != CombatStatus.Active)
                break;

            combat.AdvanceToNextCombatant();
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new TacticalCombatResponse(
            RunDto.FromDomain(run),
            TacticalCombatRuntimeDto.FromDomain(combat, CombatItemHelper.GetUsableBattleItems(run)),
            log);
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
    private IReadOnlyCollection<CombatLogEntryDto> PlayEnemyTurn(
        Domain.Combats.Tactical.TacticalCombat combat)
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
            var cost = combat.MoveActiveCombatant(destination);

            log.Add(new CombatLogEntryDto(
                OccurredAtUtc: _clock.UtcNow.UtcDateTime,
                Type: "TacticalMove",
                Message: $"{actor.DisplayName} avance en ({destination.X}, {destination.Y}) "
                         + $"pour {cost} de mouvement.",
                ActorId: actorId,
                SkillKey: null,
                TargetIds: []));
        }

        var strike = ChooseReachableSkill(combat, actor, prey);
        if (strike is null)
            return log;

        var targets = new[] { prey };
        var resolution = _effectResolver.Resolve(combat, actor, strike, targets);

        combat.MarkActiveCombatantActed();

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
