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

        if (combat.RemainingCooldown(actorId, skill.Key) > 0)
            throw new ConflictException(
                $"« {skill.DisplayName} » est encore en recharge pour "
                + $"{combat.RemainingCooldown(actorId, skill.Key)} activation(s).");

        var effectiveManaCost = Math.Max(
            0,
            (int)Math.Round(
                skill.ManaCost * (1.0 - actor.EffectiveSkillCostReductionPercent / 100.0))
            + actor.EffectiveFlatManaCostBonus);
        var missingMana = Math.Max(0, effectiveManaCost - actor.Mana);
        var vitalityCost = missingMana * 2;
        if (vitalityCost > 0 && !request.ConfirmVitalitySacrifice)
        {
            throw new ConflictException(
                $"Confirmation requise : « {skill.DisplayName} » consommera "
                + $"{vitalityCost} Vitalité pour remplacer {missingMana} Mana manquante.");
        }

        var origin = combat.PositionOf(actorId);
        var tactical = TacticalSkillProfile.For(skill);
        var requestedTarget = new GridPosition(request.TargetX, request.TargetY);
        var intendedTarget = skill.TargetingType == "Self" || tactical.AreaShape == TacticalAreaShape.Map
            ? origin
            : requestedTarget;

        if (tactical.OncePerCombat && combat.HasUsedOnceSkill(skill.Key))
            throw new ConflictException($"« {skill.DisplayName} » a déjà été utilisé dans ce combat.");

        if (!TacticalTargeting.IsInRange(
                combat.Battlefield,
                origin,
                intendedTarget,
                tactical.Range,
                tactical.RequiresLineOfSight))
        {
            throw new ConflictException(
                $"La case ({intendedTarget.X}, {intendedTarget.Y}) est hors de portée de « {skill.DisplayName} ».");
        }

        var shape = tactical.AreaShape;
        var hostile = TacticalTargeting.IsHostile(skill.TargetingType);
        var interceptor = tactical.RequiresLineOfSight && shape != TacticalAreaShape.Map
            ? TacticalTargeting.FindInterceptor(combat, origin, intendedTarget)
            : null;
        var target = interceptor is null
            ? intendedTarget
            : combat.PositionOf(interceptor.Id.Value);

        var affectedCells = shape == TacticalAreaShape.Map
            ? null
            : TacticalTargeting.CellsInArea(combat.Battlefield, target, shape);

        var targets = TacticalTargeting.ResolveTargets(
                combat, affectedCells, actor.Side, hostile, shape)
            .ToList();

        if (interceptor is not null && targets.All(t => t.Id != interceptor.Id))
            targets.Add(interceptor);

        if (targets.Count == 0)
            throw new ConflictException("Aucune cible valide dans la zone visée.");

        combat.OrientToward(actorId, target);

        // Relevé avant résolution : le noyau partagé n'annonce pas ses chiffres, on les mesure.
        var before = TacticalImpactRecorder.Capture(targets);
        var speedsBefore = combat.CaptureEffectiveSpeeds();

        var resolution = _effectResolver.Resolve(combat, actor, skill, targets);

        foreach (var affected in targets)
        {
            var facingTarget = interceptor is not null && affected.Id == interceptor.Id
                ? origin
                : shape == TacticalAreaShape.Single ? origin : target;
            combat.OrientToward(affected.Id.Value, facingTarget);
        }

        var impacts = TacticalImpactRecorder.Diff(before, targets, combat);

        if (tactical.OncePerCombat)
            combat.MarkOnceSkillUsed(skill.Key);

        combat.MarkActiveCombatantActed(skill);

        if (combat.HaveEffectiveSpeedsChanged(speedsBefore))
            combat.RecalculateInitiativeAfterSpeedChange();

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
                actorId,
                actor.DisplayName,
                skill.Key,
                skill.DisplayName,
                target,
                impacts)]);
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
