using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

/// <summary>
/// Uses a shared-inventory consumable as the active combatant's action.
/// Targeting is resolved by the same grid rules as skills, including LOS and interception.
/// </summary>
public sealed class UseTacticalItemCommandHandler
    : IRequestHandler<UseTacticalItemCommand, TacticalCombatResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly ICombatResolutionService _combatResolution;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly IClock _clock;

    public UseTacticalItemCommandHandler(
        IRunRepository runRepository,
        ICombatResolutionService combatResolution,
        IRewardOfferRepository rewardOfferRepository,
        IClock clock)
    {
        _runRepository = runRepository;
        _combatResolution = combatResolution;
        _rewardOfferRepository = rewardOfferRepository;
        _clock = clock;
    }

    public async Task<TacticalCombatResponse> Handle(
        UseTacticalItemCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);
        var combat = run.RequireActiveTacticalCombat();
        var actorId = combat.ActiveCombatantId
            ?? throw new DomainException("No combatant is currently active.");
        var actor = combat.Allies.Concat(combat.Enemies)
            .First(c => c.Id.Value == actorId);

        if (actor.Side != CombatantSide.Player)
            throw new ConflictException("Only an allied active combatant can use the shared inventory.");
        if (combat.TurnStateOf(actorId).HasActed)
            throw new ConflictException("This combatant has already acted this turn.");

        var item = run.RunItems.FirstOrDefault(i => i.Id.Value == request.ItemId)
            ?? throw new NotFoundException("Run item", request.ItemId);
        if (!item.IsUsable || !item.IsUsableInCombat)
            throw new ConflictException($"« {item.DisplayName} » cannot be used in combat.");

        var origin = combat.PositionOf(actorId);
        var requestedTarget = new GridPosition(request.TargetX, request.TargetY);
        var shape = ParseShape(item.TacticalAreaShape);

        if (item.EffectType == RunItemEffectType.RevivePercent)
        {
            var defeated = request.TargetCombatantId is { } targetId
                ? combat.Allies.FirstOrDefault(c => c.Id.Value == targetId && c.IsDefeated)
                : null;
            if (defeated is null)
                throw new ConflictException("A defeated ally must be selected for this item.");

            var before = TacticalImpactRecorder.Capture([defeated]);
            var vitality = Math.Max(1,
                (int)Math.Round(defeated.MaxVitality * item.EffectAmount / 100.0,
                    MidpointRounding.AwayFromZero));
            var destination = combat.ReviveNear(defeated.Id.Value, actorId, vitality);
            item.ConsumeOne();

            var impacts = TacticalImpactRecorder.Diff(before, [defeated], combat);
            return await PersistAndRespond(
                run, combat, actor, item, destination, [defeated], impacts, cancellationToken);
        }

        if (!TacticalTargeting.IsInRange(
                combat.Battlefield,
                origin,
                requestedTarget,
                item.TacticalRange,
                item.RequiresLineOfSight))
        {
            throw new ConflictException(
                $"La case ({requestedTarget.X}, {requestedTarget.Y}) est hors de portée de « {item.DisplayName} ».");
        }

        var interceptor = item.RequiresLineOfSight && shape != TacticalAreaShape.Map
            ? TacticalTargeting.FindInterceptor(combat, origin, requestedTarget)
            : null;
        var targetCell = interceptor is null
            ? requestedTarget
            : combat.PositionOf(interceptor.Id.Value);
        var affectedCells = shape == TacticalAreaShape.Map
            ? null
            : TacticalTargeting.CellsInArea(combat.Battlefield, targetCell, shape);
        var targets = TacticalTargeting.ResolveTargets(
                combat, affectedCells, actor.Side, hostileOnly: false, shape)
            .ToList();

        if (interceptor is not null && targets.All(t => t.Id != interceptor.Id))
            targets.Add(interceptor);
        if (targets.Count == 0)
            throw new ConflictException("Aucune cible valide dans la zone visée.");

        combat.OrientToward(actorId, targetCell);
        var beforeTargets = TacticalImpactRecorder.Capture(targets);
        ApplyEffect(combat, actor, item, targets);
        foreach (var target in targets)
            combat.OrientToward(target.Id.Value, shape == TacticalAreaShape.Single ? origin : targetCell);

        var targetImpacts = TacticalImpactRecorder.Diff(beforeTargets, targets, combat);
        item.ConsumeOne();
        combat.MarkActiveCombatantActed();

        return await PersistAndRespond(
            run, combat, actor, item, targetCell, targets, targetImpacts, cancellationToken);
    }

    private static void ApplyEffect(
        Domain.Combats.Tactical.TacticalCombat combat,
        Combatant actor,
        RunItem item,
        IReadOnlyCollection<Combatant> targets)
    {
        foreach (var target in targets.Where(t => !t.IsDefeated))
        {
            switch (item.EffectType)
            {
                case RunItemEffectType.Heal:
                    Heal(target, item.EffectAmount, actor.EffectiveHealingBonusPercent);
                    break;
                case RunItemEffectType.HealPercent:
                case RunItemEffectType.ConditionalHealOrPoison:
                    HealPercent(target, item.EffectAmount, actor.EffectiveHealingBonusPercent);
                    break;
                case RunItemEffectType.HealPercentAndCleanseDot:
                    HealPercent(target, item.EffectAmount, actor.EffectiveHealingBonusPercent);
                    target.DispelOneStatusStack(e => e.Kind == StatusEffectKind.DamageOverTime);
                    break;
                case RunItemEffectType.HealPercentAndSilence:
                    HealPercent(target, item.EffectAmount, actor.EffectiveHealingBonusPercent);
                    target.ApplyStatusEffect(CombatStatusEffect.Create(
                        "item.status.silence",
                        "Silence",
                        StatusEffectKind.Silence,
                        combat.CurrentTick,
                        CombatTime.TicksPerTurn));
                    break;
                case RunItemEffectType.HealPercentAndEvasion:
                    HealPercent(target, item.EffectAmount, actor.EffectiveHealingBonusPercent);
                    target.ApplyStatusEffect(CombatStatusEffect.Create(
                        "item.status.brume-evasion",
                        "Brume d'esquive",
                        StatusEffectKind.StatModifier,
                        combat.CurrentTick,
                        int.MaxValue - combat.CurrentTick,
                        magnitude: 15,
                        stat: CombatStat.Evasion));
                    break;
                case RunItemEffectType.ManaRestore:
                    target.GainMana(item.EffectAmount);
                    break;
                case RunItemEffectType.ChargeRestore:
                    target.GainCharge(item.EffectAmount);
                    break;
                case RunItemEffectType.Guard:
                    target.GainGuard(item.EffectAmount);
                    break;
                case RunItemEffectType.HealAndManaRestorePercent:
                    HealPercent(target, item.EffectAmount, actor.EffectiveHealingBonusPercent);
                    target.GainMana(Math.Max(1,
                        (int)Math.Round(target.MaxMana * item.EffectAmount / 100.0,
                            MidpointRounding.AwayFromZero)));
                    break;
                default:
                    throw new ConflictException(
                        $"L'effet de « {item.DisplayName} » n'est pas une action consommable résolue par le combat tactique.");
            }
        }
    }

    private static void HealPercent(Combatant target, int percent, int bonusPercent)
        => Heal(target,
            Math.Max(1, (int)Math.Round(target.MaxVitality * percent / 100.0,
                MidpointRounding.AwayFromZero)),
            bonusPercent);

    private static void Heal(Combatant target, int amount, int bonusPercent)
    {
        var resolved = Math.Max(1,
            (int)Math.Round(amount * (1.0 + bonusPercent / 100.0),
                MidpointRounding.AwayFromZero));
        if (target.CurrentVitality < target.MaxVitality)
            target.ApplyHeal(resolved);
    }

    private async Task<TacticalCombatResponse> PersistAndRespond(
        Run run,
        Domain.Combats.Tactical.TacticalCombat combat,
        Combatant actor,
        RunItem item,
        GridPosition target,
        IReadOnlyCollection<Combatant> targets,
        IReadOnlyList<TacticalImpactDto> impacts,
        CancellationToken cancellationToken)
    {
        var protagonist = combat.Allies.FirstOrDefault(a => a.Side == CombatantSide.Player);
        if (protagonist is not null)
        {
            run.PlayerState.SyncFromCombat(
                protagonist.CurrentVitality, protagonist.Guard, protagonist.Mana, protagonist.Charge);
        }

        var rewardOffer = await _combatResolution.ApplyOutcomeAsync(
            run, combat, _clock.UtcNow, cancellationToken);
        await _runRepository.UpdateAsync(run, cancellationToken);
        if (rewardOffer is not null)
            await _rewardOfferRepository.AddAsync(run.Id, rewardOffer, cancellationToken);

        var log = new CombatLogEntryDto(
            _clock.UtcNow.UtcDateTime,
            "TacticalItemUsed",
            $"{actor.DisplayName} utilise « {item.DisplayName} ».",
            actor.Id.Value,
            item.DefinitionKey,
            [.. targets.Select(t => t.Id.Value)]);

        return new TacticalCombatResponse(
            RunDto.FromDomain(run),
            TacticalCombatRuntimeDto.FromDomain(combat, CombatItemHelper.GetUsableBattleItems(run)),
            [log],
            [TacticalCombatEventDto.Item(
                actor.Id.Value,
                actor.DisplayName,
                item.DefinitionKey,
                item.DisplayName,
                target,
                impacts)]);
    }

    private static TacticalAreaShape ParseShape(string shape)
        => Enum.TryParse<TacticalAreaShape>(shape, true, out var parsed)
            ? parsed
            : TacticalAreaShape.Diamond;
}
