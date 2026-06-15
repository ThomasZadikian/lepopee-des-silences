using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseItemInCombat;

public sealed class UseItemInCombatCommandHandler
    : IRequestHandler<UseItemInCombatCommand, CombatSkillActionResult>
{
    private readonly IRunRepository _runRepository;
    private readonly IEnemyCombatTurnResolver _enemyTurnResolver;
    private readonly IClock _clock;

    public UseItemInCombatCommandHandler(
        IRunRepository runRepository,
        IEnemyCombatTurnResolver enemyTurnResolver,
        IClock clock)
    {
        _runRepository = runRepository;
        _enemyTurnResolver = enemyTurnResolver;
        _clock = clock;
    }

    public async Task<CombatSkillActionResult> Handle(
        UseItemInCombatCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);
        var combatId = new CombatId(request.CombatId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        // ── Validations combat ─────────────────────────────────────────────

        if (run.Status != RunStatus.Active)
            throw new ConflictException("Run must be active to use an item in combat.");

        if (!run.HasActiveCombat || run.ActiveCombat is null)
            throw new ConflictException("Run has no active combat.");

        if (run.ActiveCombat.Id != combatId)
            throw new ConflictException("Combat does not match the active run combat.");

        var playerCombatant = run.ActiveCombat.Allies
            .FirstOrDefault(a => a.Side == CombatantSide.Player)
            ?? throw new ConflictException("No player combatant found in this combat.");

        run.ActiveCombat.EnsureActorCanAct(playerCombatant.Id.Value);

        // ── Validation de l'objet ──────────────────────────────────────────

        var itemId = new RunItemId(request.ItemId);
        var item = run.RunItems.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Item", request.ItemId);

        if (!item.IsBattleItem)
            throw new ConflictException(
                $"Item '{item.DefinitionKey}' cannot be used in combat.");

        if (item.Type != RunItemType.Consumable || item.Quantity <= 0)
            throw new ConflictException(
                $"Item '{item.DefinitionKey}' has no remaining uses.");

        // ── Application de l'effet sur le combattant ───────────────────────

        var targets = ResolveTargets(item, playerCombatant, run.ActiveCombat, request.TargetIds);
        ApplyItemEffect(item, targets);
        item.ConsumeOne();

        var now = _clock.UtcNow;
        var logs = new List<CombatLogEntryDto>();

        // Entry d'utilisation
        logs.Add(new CombatLogEntryDto(
            OccurredAtUtc: now.DateTime,
            Type: "ItemUsed",
            Message: $"{playerCombatant.DisplayName} utilise {item.DisplayName}.",
            ActorId: playerCombatant.Id.Value,
            SkillKey: item.DefinitionKey,
            TargetIds: targets.Select(t => t.Id.Value).ToArray()));

        // Entry d'effet immédiat — permet au frontend d'appliquer
        // le changement visuellement avant les tours ennemis.
        foreach (var target in targets)
        {
            var effectEntry = item.EffectType switch
            {
                RunItemEffectType.Heal => new CombatLogEntryDto(
                    OccurredAtUtc: now.DateTime,
                    Type: "HealApplied",
                    Message: $"{target.DisplayName} récupère {item.EffectAmount} PV.",
                    ActorId: playerCombatant.Id.Value,
                    SkillKey: item.DefinitionKey,
                    TargetIds: [target.Id.Value]),

                RunItemEffectType.Guard => new CombatLogEntryDto(
                    OccurredAtUtc: now.DateTime,
                    Type: "GuardGained",
                    Message: $"{target.DisplayName} gagne {item.EffectAmount} points de garde.",
                    ActorId: playerCombatant.Id.Value,
                    SkillKey: item.DefinitionKey,
                    TargetIds: [target.Id.Value]),

                RunItemEffectType.ManaRestore => new CombatLogEntryDto(
                    OccurredAtUtc: now.DateTime,
                    Type: "ManaRestored",
                    Message: $"{target.DisplayName} récupère {item.EffectAmount} points de mana.",
                    ActorId: playerCombatant.Id.Value,
                    SkillKey: item.DefinitionKey,
                    TargetIds: [target.Id.Value]),

                RunItemEffectType.ChargeRestore => new CombatLogEntryDto(
                    OccurredAtUtc: now.DateTime,
                    Type: "ChargeRestored",
                    Message: $"{target.DisplayName} récupère {item.EffectAmount} points de charge.",
                    ActorId: playerCombatant.Id.Value,
                    SkillKey: item.DefinitionKey,
                    TargetIds: [target.Id.Value]),

                _ => null
            };

            if (effectEntry is not null)
                logs.Add(effectEntry);
        }

        // ── Avance de tour + tours ennemis ─────────────────────────────────

        var combat = run.ActiveCombat;
        logs.AddRange(AdvanceCombat(combat, now.DateTime));
        logs.AddRange(ResolveEnemyTurns(combat));

        SyncPlayerStateFromCombat(run, combat);

        var combatCompleted = combat.Status == CombatStatus.Completed;
        var combatFailed = combat.Status == CombatStatus.Failed;

        if (combatCompleted) run.CompleteActiveCombat();
        else if (combatFailed) run.FailActiveCombat(now);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new CombatSkillActionResult(
            CombatId: combat.Id.Value,
            ActorId: playerCombatant.Id.Value,
            SkillKey: item.DefinitionKey,
            TargetIds: targets.Select(t => t.Id.Value).ToArray(),
            Accepted: true,
            Message: null,
            Combat: CombatRuntimeDto.FromDomain(combat, CombatItemHelper.GetUsableBattleItems(run)),
            LogEntries: logs,
            CombatCompleted: combatCompleted,
            CombatFailed: combatFailed,
            CanProgressRun: combatCompleted,
            RunStatus: run.Status.ToString());
    }

    // ── Helpers privés ─────────────────────────────────────────────────────

    private static IReadOnlyCollection<Combatant> ResolveTargets(
        RunItem item,
        Combatant playerCombatant,
        Combat combat,
        IReadOnlyCollection<Guid> requestedTargetIds)
    {
        return item.BattleTargetingType switch
        {
            "SingleAlly" when requestedTargetIds.Count > 0 =>
                combat.Allies
                    .Where(a => requestedTargetIds.Contains(a.Id.Value) && !a.IsDefeated)
                    .Take(1)
                    .ToArray(),

            "SingleAlly" => [playerCombatant],

            _ => [playerCombatant], // Self
        };
    }

    private static void ApplyItemEffect(
        RunItem item,
        IReadOnlyCollection<Combatant> targets)
    {
        foreach (var target in targets)
        {
            switch (item.EffectType)
            {
                case RunItemEffectType.Heal:
                    target.ApplyHeal(item.EffectAmount);
                    break;

                case RunItemEffectType.Guard:
                    target.GainGuard(item.EffectAmount);
                    break;

                case RunItemEffectType.ManaRestore:
                    target.GainMana(item.EffectAmount);
                    break;

                case RunItemEffectType.ChargeRestore:
                    target.GainCharge(item.EffectAmount);
                    break;
            }
        }
    }

    private static IReadOnlyCollection<CombatLogEntryDto> AdvanceCombat(
        Combat combat,
        DateTime now)
    {
        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        if (combat.Status != CombatStatus.Active) return [];

        combat.AdvanceTurn();
        return [];
    }

    private IReadOnlyCollection<CombatLogEntryDto> ResolveEnemyTurns(Combat combat)
    {
        var logs = new List<CombatLogEntryDto>();
        var maxTurns = combat.Allies.Concat(combat.Enemies).Count(c => !c.IsDefeated) + 1;
        var count = 0;

        while (combat.Status == CombatStatus.Active)
        {
            var active = combat.GetActiveCombatant();
            if (active is null || active.Side != CombatantSide.Enemy) break;
            if (count++ >= maxTurns) break;

            var resolution = _enemyTurnResolver.Resolve(combat);
            logs.AddRange(resolution.LogEntries);
            if (!resolution.WasResolved) break;
        }

        return logs;
    }

    private static void SyncPlayerStateFromCombat(Run run, Combat combat)
    {
        var player = combat.Allies.FirstOrDefault(a => a.Side == CombatantSide.Player);
        if (player is null) return;

        run.PlayerState.SyncFromCombat(
            player.CurrentVitality,
            player.Guard,
            player.Mana,
            player.Charge);
    }
}