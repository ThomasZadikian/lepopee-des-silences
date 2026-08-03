using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Tactical;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Domain.Rewards;
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
/// tour n'apporterait qu'un aller-retour réseau par ennemi, sans rien changer au résultat.
/// L'enchaînement lui-même vit dans <see cref="ITacticalEnemyTurnDriver"/>, parce que
/// l'ouverture du combat en a besoin autant que la fin de tour.
/// </para>
/// <para>
/// La contrepartie, c'est que le client recevrait un saut d'état incompréhensible : figures
/// téléportées, dégâts sans cause. D'où la chronologie renvoyée à côté de l'état final — la
/// décision est prise et définitive, seule sa mise en scène reste à jouer, au rythme du client.
/// </para>
/// </remarks>
public sealed class EndTacticalTurnCommandHandler
    : IRequestHandler<EndTacticalTurnCommand, TacticalCombatResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly ITacticalEnemyTurnDriver _enemyTurns;
    private readonly ICombatResolutionService _combatResolution;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly IClock _clock;

    public EndTacticalTurnCommandHandler(
        IRunRepository runRepository,
        ITacticalEnemyTurnDriver enemyTurns,
        ICombatResolutionService combatResolution,
        IRewardOfferRepository rewardOfferRepository,
        IClock clock)
    {
        _runRepository = runRepository;
        _enemyTurns = enemyTurns;
        _combatResolution = combatResolution;
        _rewardOfferRepository = rewardOfferRepository;
        _clock = clock;
    }

    public async Task<TacticalCombatResponse> Handle(
        EndTacticalTurnCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var combat = run.RequireActiveTacticalCombat();

        // L'issue se fige à la fin du tour actif. La défaite a priorité si un
        // sacrifice élimine simultanément le dernier allié et le dernier ennemi.
        combat.FailIfAllAlliesDefeated();
        combat.CompleteIfAllEnemiesDefeated();

        TacticalCombatEventDto? handoffTick = null;
        if (combat.Status == CombatStatus.Active)
        {
            if (combat.ActiveCombatantId is { } activeId
                && !combat.TurnStateOf(activeId).HasActed)
            {
                combat.ConsumeBasicAttackRestriction();
            }
            combat.AdvanceToNextCombatant();
            handoffTick = TacticalImpactRecorder.BuildTickEvent(combat);
        }

        var enemyTurns = combat.Status == CombatStatus.Active
            ? _enemyTurns.PlayWhileEnemyHasInitiative(combat)
            : new TacticalEnemyTurnsResult([], []);

        var events = handoffTick is null
            ? enemyTurns.Events
            : (IReadOnlyList<TacticalCombatEventDto>)[handoffTick, .. enemyTurns.Events];

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
        {
            await _rewardOfferRepository.AddAsync(run.Id, rewardOffer, cancellationToken);
        }

        return new TacticalCombatResponse(
            RunDto.FromDomain(run),
            TacticalCombatRuntimeDto.FromDomain(combat, CombatItemHelper.GetUsableBattleItems(run, combat)),
            enemyTurns.LogEntries,
            events);
    }
}
