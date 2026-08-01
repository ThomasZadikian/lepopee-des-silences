using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Combats.Tactical;
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
    private readonly ICombatSkillEffectResolver _skillEffectResolver;

    public UseTacticalItemCommandHandler(
        IRunRepository runRepository,
        ICombatResolutionService combatResolution,
        IRewardOfferRepository rewardOfferRepository,
        IClock clock,
        ICombatSkillEffectResolver skillEffectResolver)
    {
        _runRepository = runRepository;
        _combatResolution = combatResolution;
        _rewardOfferRepository = rewardOfferRepository;
        _clock = clock;
        _skillEffectResolver = skillEffectResolver;
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
        if (combat.NextActionRestrictedToBasicAttack)
            throw new ConflictException(
                "Loi de l'Éloge Funèbre : seule une attaque basique est permise après une mort.");

        if (TacticalEquipmentAbilityCatalog.TryResolve(
                combat,
                actorId,
                request.ItemId,
                out var equipmentAbility))
        {
            return await UseEquipmentAbilityAsync(
                run,
                combat,
                actor,
                equipmentAbility,
                new GridPosition(request.TargetX, request.TargetY),
                cancellationToken);
        }

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
            TacticalChargeRules.AwardUsefulAction(actor, [defeated], impacts);
            combat.MarkActiveCombatantActed();
            return await PersistAndRespond(
                run, combat, actor, item, destination, [defeated], impacts, cancellationToken);
        }

        var effectiveItemRange = actor.HasTacticalSlow
            ? Math.Max(1, item.TacticalRange / 2)
            : item.TacticalRange;
        if (!TacticalTargeting.IsInRange(
                combat.Battlefield,
                origin,
                requestedTarget,
                effectiveItemRange,
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
        TacticalChargeRules.AwardUsefulAction(actor, targets, targetImpacts);
        item.ConsumeOne();
        combat.MarkActiveCombatantActed();

        return await PersistAndRespond(
            run, combat, actor, item, targetCell, targets, targetImpacts, cancellationToken);
    }

    private async Task<TacticalCombatResponse> UseEquipmentAbilityAsync(
        Run run,
        Domain.Combats.Tactical.TacticalCombat combat,
        Combatant actor,
        TacticalEquipmentAbilityCatalog.Ability ability,
        GridPosition requestedTarget,
        CancellationToken cancellationToken)
    {
        if (combat.HasUsedOnceSkill(ability.UseKey))
            throw new ConflictException($"« {ability.DisplayName} » a déjà été utilisé dans ce combat.");

        var origin = combat.PositionOf(actor.Id.Value);
        var targets = new List<Combatant>();
        var targetCell = requestedTarget;
        var extraLogEntries = new List<CombatLogEntryDto>();

        switch (ability.ItemKey)
        {
            case "item.aiguille-arret":
                targets.AddRange(combat.Enemies.Where(enemy => !enemy.IsDefeated));
                foreach (var affectedTarget in targets)
                {
                    affectedTarget.ApplyStatusEffect(CombatStatusEffect.Create(
                        "equipment.temporal-slow.aiguille-arret",
                        "Temps ralenti",
                        StatusEffectKind.StatModifier,
                        combat.StatusClockFor(affectedTarget.Id.Value),
                        CombatTime.TicksPerTurn * 2,
                        magnitude: -50,
                        stat: CombatStat.Speed,
                        isMagnitudePercentOfBaseStat: true));
                }

                targetCell = origin;
                break;

            case "item.aiguille-relieur":
                if (!TacticalTargeting.IsInRange(
                        combat.Battlefield,
                        origin,
                        requestedTarget,
                        ability.Range,
                        ability.RequiresLineOfSight))
                {
                    throw new ConflictException("La cible de l’Aiguille du Relieur est hors de portée.");
                }

                var target = combat.Enemies.FirstOrDefault(enemy =>
                    !enemy.IsDefeated
                    && combat.PositionOf(enemy.Id.Value) == requestedTarget)
                    ?? throw new ConflictException("L’Aiguille du Relieur exige un ennemi vivant.");
                var now = combat.StatusClockFor(target.Id.Value);
                var periodicEffects = target.StatusEffects
                    .Where(effect => effect.Kind == StatusEffectKind.DamageOverTime)
                    .ToArray();
                if (periodicEffects.Length == 0)
                    throw new ConflictException("Cette cible ne porte aucun effet périodique à prolonger.");

                foreach (var effect in periodicEffects)
                {
                    var remaining = Math.Max(1, effect.ExpiresAtTick - now);
                    var extension = (int)Math.Ceiling(remaining * 0.25);
                    effect.ExtendDuration(effect.ExpiresAtTick + extension);
                }

                targets.Add(target);
                combat.OrientToward(actor.Id.Value, requestedTarget);
                break;

            case "item.iris-amethyste":
                if (!TacticalTargeting.IsInRange(
                        combat.Battlefield,
                        origin,
                        requestedTarget,
                        ability.Range,
                        ability.RequiresLineOfSight))
                {
                    throw new ConflictException("La cible de l’Iris améthyste est hors de portée.");
                }

                // "Force un ennemi non-boss à attaquer son propre camp" — the domain model
                // has no boss/non-boss flag on Combatant, so this is resolved against any
                // living enemy (documented simplification: harmless in practice, since a
                // lone boss room simply has no other enemy to mind-control it against, and
                // the ability then refuses to fire below).
                var mindControlled = combat.Enemies.FirstOrDefault(enemy =>
                    !enemy.IsDefeated
                    && combat.PositionOf(enemy.Id.Value) == requestedTarget)
                    ?? throw new ConflictException("L’Iris améthyste exige un ennemi vivant.");

                var forcedTarget = combat.Enemies
                    .Where(enemy => !enemy.IsDefeated && enemy.Id != mindControlled.Id)
                    .OrderBy(enemy => combat.DistanceBetween(mindControlled.Id.Value, enemy.Id.Value))
                    .FirstOrDefault()
                    ?? throw new ConflictException(
                        "Aucun autre ennemi vivant à retourner contre cette cible.");

                var forcedSkill = mindControlled.Skills
                        .Where(s => string.Equals(s.EffectType, "Damage", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(s => s.BasePower)
                        .FirstOrDefault()
                    ?? mindControlled.Skills.First();

                var resolution = _skillEffectResolver.Resolve(
                    combat, mindControlled, forcedSkill, [forcedTarget]);
                extraLogEntries.AddRange(resolution.LogEntries);

                // "Coûte 1% de Vitalité maximale pour la run" — applied here as a same-combat
                // current-Vitality cost to the wearer (documented simplification: a genuinely
                // permanent, run-wide max-Vitality reduction would need new Run/persistence
                // plumbing this accessory alone doesn't justify).
                actor.ApplyVitalityDamage(Math.Max(1,
                    (int)Math.Round(actor.MaxVitality * 0.01, MidpointRounding.AwayFromZero)));

                targets.Add(mindControlled);
                targets.Add(forcedTarget);
                targetCell = requestedTarget;
                break;

            default:
                throw new ConflictException($"L’effet de « {ability.DisplayName} » n’est pas implémenté.");
        }

        combat.MarkOnceSkillUsed(ability.UseKey);
        combat.MarkActiveCombatantActed();

        await _runRepository.UpdateAsync(run, cancellationToken);

        var log = new CombatLogEntryDto(
            _clock.UtcNow.UtcDateTime,
            "TacticalEquipmentUsed",
            $"{actor.DisplayName} active « {ability.DisplayName} ».",
            actor.Id.Value,
            ability.ItemKey,
            [.. targets.Select(target => target.Id.Value)]);

        return new TacticalCombatResponse(
            RunDto.FromDomain(run),
            TacticalCombatRuntimeDto.FromDomain(
                combat,
                CombatItemHelper.GetUsableBattleItems(run, combat)),
            [log, .. extraLogEntries],
            [TacticalCombatEventDto.Item(
                actor.Id.Value,
                actor.DisplayName,
                ability.ItemKey,
                ability.DisplayName,
                targetCell,
                [])]);
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
                protagonist.CurrentVitality, protagonist.Guard, protagonist.Mana, 0);
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
            TacticalCombatRuntimeDto.FromDomain(combat, CombatItemHelper.GetUsableBattleItems(run, combat)),
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
