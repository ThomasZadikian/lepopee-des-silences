using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Effects;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseRunItem;

public sealed class UseRunItemCommandHandler
    : IRequestHandler<UseRunItemCommand, UseRunItemResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly IEnemyCombatTurnResolver _enemyTurnResolver;
    private readonly IRuntimeEffectResolver _runtimeEffectResolver;
    private readonly ICatalogEffectSetProvider _catalogEffectSetProvider;
    private readonly ICatalogItemDefinitionProvider _catalogItemDefinitionProvider;

    public UseRunItemCommandHandler(
        IRunRepository runRepository,
        IEnemyCombatTurnResolver enemyTurnResolver,
        IRuntimeEffectResolver runtimeEffectResolver,
        ICatalogEffectSetProvider catalogEffectSetProvider,
        ICatalogItemDefinitionProvider catalogItemDefinitionProvider)
    {
        _runRepository = runRepository;
        _enemyTurnResolver = enemyTurnResolver;
        _runtimeEffectResolver = runtimeEffectResolver;
        _catalogEffectSetProvider = catalogEffectSetProvider;
        _catalogItemDefinitionProvider = catalogItemDefinitionProvider;
    }

    public async Task<UseRunItemResponse> Handle(
        UseRunItemCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);
        var itemId = new RunItemId(request.ItemId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var wasInCombat = run.HasActiveCombat;

        var (effectType, amount, depleted) = run.UseItem(itemId);

        // En combat, l'utilisation d'un objet compte comme l'action du joueur :
        // avance le tour et résout les tours ennemis automatiquement.
        IReadOnlyCollection<CombatLogEntryDto>? logEntries = null;
        CombatRuntimeDto? combatDto = null;

        if (wasInCombat && run.ActiveCombat is not null)
        {
            var combat = run.ActiveCombat;
            var logs = new List<CombatLogEntryDto>();

            logs.AddRange(AdvanceCombat(combat));

            if (combat.Status == CombatStatus.Active)
                logs.AddRange(ResolveEnemyTurns(combat));

            SyncPlayerStateFromCombat(run, combat);

            logEntries = logs;
            CombatRuntimeDto.FromDomain(combat, CombatItemHelper.GetUsableBattleItems(run));
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new UseRunItemResponse(
            RunId: run.Id.Value,
            ItemId: request.ItemId,
            EffectType: effectType.ToString(),
            EffectAmount: amount,
            ItemDepleted: depleted,
            UsedInCombat: wasInCombat,
            PlayerState: PlayerRuntimeStateDto.FromDomain(run.PlayerState),
            Combat: combatDto,
            LogEntries: logEntries);
    }

    private static IReadOnlyCollection<CombatLogEntryDto> AdvanceCombat(Combat combat)
    {
        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        if (combat.Status != CombatStatus.Active)
            return [];

        combat.AdvanceTurn();
        return [];
    }

    private IReadOnlyCollection<CombatLogEntryDto> ResolveEnemyTurns(Combat combat)
    {
        var logs = new List<CombatLogEntryDto>();
        var maxAutoTurns = combat.Allies.Concat(combat.Enemies).Count(c => !c.IsDefeated) + 1;
        var count = 0;

        while (combat.Status == CombatStatus.Active)
        {
            var active = combat.GetActiveCombatant();
            if (active is null || active.Side != CombatantSide.Enemy) break;
            if (count++ >= maxAutoTurns) break;

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