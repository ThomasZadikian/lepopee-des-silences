using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

/// <summary>
/// Fait agir le combattant actif. Le noyau de résolution est <b>exactement celui de l'ATB</b> :
/// dégâts, statuts, DoT et Lois passent par <see cref="ICombatSkillEffectResolver"/>, qui ne
/// connaît que <see cref="ICombatContext"/> et ignore tout de l'ordonnancement.
/// </summary>
/// <remarks>
/// Ce que ce handler ajoute par-dessus, et que l'ATB n'a pas : la portée, la ligne de vue et la
/// zone d'effet. C'est là, et seulement là, que le positionnement entre en jeu.
/// </remarks>
public sealed class UseTacticalSkillCommandHandler
    : IRequestHandler<UseTacticalSkillCommand, TacticalCombatResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly ICombatSkillEffectResolver _effectResolver;
    private readonly ICombatResolutionService _combatResolution;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly IClock _clock;

    public UseTacticalSkillCommandHandler(
        IRunRepository runRepository,
        ICombatSkillEffectResolver effectResolver,
        ICombatResolutionService combatResolution,
        IRewardOfferRepository rewardOfferRepository,
        IClock clock)
    {
        _runRepository = runRepository;
        _effectResolver = effectResolver;
        _combatResolution = combatResolution;
        _rewardOfferRepository = rewardOfferRepository;
        _clock = clock;
    }

    public async Task<TacticalCombatResponse> Handle(
        UseTacticalSkillCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var combat = run.RequireActiveTacticalCombat();

        var actorId = combat.ActiveCombatantId
            ?? throw new DomainException("No combatant is currently active.");

        var actor = combat.Allies.Concat(combat.Enemies)
            .FirstOrDefault(c => c.Id.Value == actorId)
            ?? throw new DomainException($"Combatant '{actorId}' is not part of this combat.");

        if (combat.TurnStateOf(actorId).HasActed)
            throw new ConflictException("This combatant has already acted this turn.");

        var skill = actor.Skills.FirstOrDefault(s =>
            string.Equals(s.Key, request.SkillKey, StringComparison.Ordinal))
            ?? throw new DomainException(
                $"'{actor.DisplayName}' does not know skill '{request.SkillKey}'.");

        var origin = combat.PositionOf(actorId);
        var target = new GridPosition(request.TargetX, request.TargetY);

        var tactical = TacticalSkillProfile.For(skill);

        if (!TacticalTargeting.IsInRange(
                combat.Battlefield,
                origin,
                target,
                tactical.Range,
                tactical.RequiresLineOfSight))
        {
            throw new ConflictException(
                $"La case ({target.X}, {target.Y}) est hors de portée de « {skill.DisplayName} ».");
        }

        var shape = tactical.AreaShape;
        var hostile = TacticalTargeting.IsHostile(skill.TargetingType);

        var affectedCells = shape == TacticalAreaShape.Map
            ? null
            : TacticalTargeting.CellsInArea(combat.Battlefield, target, shape);

        var targets = TacticalTargeting.ResolveTargets(
            combat, affectedCells, actor.Side, hostile, shape);

        if (targets.Count == 0)
            throw new ConflictException("Aucune cible valide dans la zone visée.");

        // Relevé avant résolution : le noyau partagé n'annonce pas ses chiffres, on les mesure.
        var before = TacticalImpactRecorder.Capture(targets);

        var resolution = _effectResolver.Resolve(combat, actor, skill, targets);

        var impacts = TacticalImpactRecorder.Diff(before, targets, combat);

        combat.MarkActiveCombatantActed();

        // Un combat gagné ou perdu ne doit pas rester « actif » : le reste de la pile (offre de
        // récompense, sortie de salle) se déclenche sur le statut, comme en ATB.
        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        var rewardOffer = await SettleAsync(run, combat, cancellationToken);

        await _runRepository.UpdateAsync(run, cancellationToken);

        if (rewardOffer is not null)
        {
            await _rewardOfferRepository.AddAsync(run.Id, rewardOffer, cancellationToken);
        }

        var actionEntry = new CombatLogEntryDto(
            OccurredAtUtc: _clock.UtcNow.UtcDateTime,
            Type: "ActionAccepted",
            Message: $"« {skill.DisplayName} » lancé par {actor.DisplayName}.",
            ActorId: actorId,
            SkillKey: skill.Key,
            TargetIds: [.. targets.Select(t => t.Id.Value)]);

        return new TacticalCombatResponse(
            RunDto.FromDomain(run),
            TacticalCombatRuntimeDto.FromDomain(combat, CombatItemHelper.GetUsableBattleItems(run)),
            [actionEntry, .. resolution.LogEntries],
            [TacticalCombatEventDto.Skill(
                actorId, actor.DisplayName, skill.Key, skill.DisplayName, impacts)]);
    }

    /// <summary>
    /// Reporte l'état du protagoniste dans la run, puis applique l'issue du combat s'il vient
    /// de s'achever.
    /// </summary>
    /// <remarks>
    /// Sans cet appel, un combat tactique gagné laissait la run figée : le statut de l'agrégat
    /// passait bien à « Completed », mais rien ne créait l'offre de récompense ni ne rendait la
    /// main à l'exploration. C'est exactement ce que fait la pile ATB après chaque action ; le
    /// service de résolution est commun aux deux depuis qu'il prend un `ICombatContext`.
    /// </remarks>
    private async Task<RewardOffer?> SettleAsync(
        Run run,
        Domain.Combats.Tactical.TacticalCombat combat,
        CancellationToken cancellationToken)
    {
        var protagonist = combat.Allies.FirstOrDefault(a => a.Side == CombatantSide.Player);
        if (protagonist is not null)
        {
            run.PlayerState.SyncFromCombat(
                protagonist.CurrentVitality, protagonist.Guard, protagonist.Mana, protagonist.Charge);
        }

        return await _combatResolution.ApplyOutcomeAsync(
            run, combat, _clock.UtcNow, cancellationToken);
    }
}
