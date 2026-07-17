using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using MediatR;

namespace Leds.GameEngine.Application.Runs.Reposition;

/// <summary>
/// Reposition action: switches the actor between Front and Back row mid-combat.
/// Costs the actor's whole turn — modelled with the same recovery cost as the
/// basic attack ("skill.basic.strike", BasePower 10), since no combat skill
/// backs this action.
/// </summary>
public sealed class RepositionCommandHandler
    : IRequestHandler<RepositionCommand, CombatSkillActionResult>
{
    private const string RepositionActionKey = "action.reposition";
    private const int RepositionBasePower = 10;

    private readonly IRunRepository _runRepository;
    private readonly IClock _clock;
    private readonly ICombatResolutionService _combatResolution;

    public RepositionCommandHandler(
        IRunRepository runRepository,
        IClock clock,
        ICombatResolutionService combatResolution)
    {
        _runRepository = runRepository;
        _clock = clock;
        _combatResolution = combatResolution;
    }

    public async Task<CombatSkillActionResult> Handle(
        RepositionCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.Status != RunStatus.Active)
        {
            throw new ConflictException("Run must be active to submit a combat action.");
        }

        if (!run.HasActiveCombat || run.ActiveCombat is null)
        {
            throw new ConflictException("Run has no active combat.");
        }

        if (run.ActiveCombat.Id != new CombatId(request.CombatId))
        {
            throw new ConflictException("Combat does not match the active run combat.");
        }

        var combat = run.ActiveCombat;

        combat.EnsureActorCanAct(request.ActorId);

        var actor = combat.Allies.Concat(combat.Enemies)
            .FirstOrDefault(c => c.Id.Value == request.ActorId)
            ?? throw new ConflictException("Actor does not exist in this combat.");

        var row = Enum.Parse<CombatRow>(request.Row, ignoreCase: true);

        actor.SetRow(row);

        combat.RegisterActionTaken(
            actor.Id.Value,
            AtbActionMath.RecoveryTicks(RepositionBasePower, actor.BaseStatSnapshot.Recovery));

        var now = _clock.UtcNow;

        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        if (combat.Status == CombatStatus.Active)
        {
            combat.ElectActiveByReadiness();
        }

        var combatCompleted = combat.Status == CombatStatus.Completed;
        var combatFailed = combat.Status == CombatStatus.Failed;

        var rewardOffer = await _combatResolution.ApplyOutcomeAsync(run, combat, now, cancellationToken);

        if (combatCompleted || combatFailed || rewardOffer is not null)
        {
            await _runRepository.UpdateAsync(run, cancellationToken);
        }
        else
        {
            await _runRepository.UpdateActiveCombatStateAsync(run, cancellationToken);
        }

        var logEntry = new CombatLogEntryDto(
            OccurredAtUtc: now.UtcDateTime,
            Type: "RepositionDeclared",
            Message: $"{actor.DisplayName} se repositionne en rang {row}.",
            ActorId: actor.Id.Value,
            SkillKey: RepositionActionKey,
            TargetIds: [actor.Id.Value]);

        return new CombatSkillActionResult(
            CombatId: combat.Id.Value,
            ActorId: request.ActorId,
            SkillKey: RepositionActionKey,
            TargetIds: [actor.Id.Value],
            Accepted: true,
            Message: null,
            Combat: CombatRuntimeDto.FromDomain(combat, CombatItemHelper.GetUsableBattleItems(run)),
            LogEntries: [logEntry],
            CombatCompleted: combatCompleted,
            CombatFailed: combatFailed,
            CanProgressRun: combatCompleted,
            RunStatus: run.Status.ToString());
    }
}
