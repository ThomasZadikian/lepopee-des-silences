using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseCombatSkill;

public sealed class UseCombatSkillCommandHandler
    : IRequestHandler<UseCombatSkillCommand, CombatSkillActionResult>
{
    private readonly IRunRepository _runRepository;
    private readonly ICombatSkillActionValidator _validator;
    private readonly ICombatSkillEffectResolver _effectResolver;
    private readonly IClock _clock;

    public UseCombatSkillCommandHandler(
        IRunRepository runRepository,
        ICombatSkillActionValidator validator,
        ICombatSkillEffectResolver effectResolver,
        IClock clock)
    {
        _runRepository = runRepository;
        _validator = validator;
        _effectResolver = effectResolver;
        _clock = clock;
    }

    public async Task<CombatSkillActionResult> Handle(
        UseCombatSkillCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.Status != RunStatus.Active)
        {
            throw new DomainException("Run must be active to submit a combat action.");
        }

        if (!run.HasActiveCombat)
        {
            throw new DomainException("Run has no active combat.");
        }

        var combatId = new CombatId(request.CombatId);

        if (run.ActiveCombat is null)
        {
            throw new DomainException("Run has no active combat.");
        }

        if (run.ActiveCombat.Id != combatId)
        {
            throw new DomainException("Combat does not match the active run combat.");
        }

        run.ActiveCombat.EnsureActorCanAct(request.ActorId);

        var validationResult = _validator.Validate(
            run.ActiveCombat,
            request.ActorId,
            request.SkillKey,
            request.TargetIds);

        if (!validationResult.IsValid)
        {
            throw new DomainException(validationResult.ErrorMessage!);
        }

        var now = _clock.UtcNow;
        var resolvedTargetIds = validationResult.Targets
            .Select(t => t.Id.Value)
            .ToArray();

        var logEntry = new CombatLogEntryDto(
            OccurredAtUtc: now.DateTime,
            Type: "ActionAccepted",
            Message: $"Skill '{request.SkillKey}' used by actor '{request.ActorId}'.",
            ActorId: request.ActorId,
            SkillKey: request.SkillKey,
            TargetIds: resolvedTargetIds);

        var effectResolution = _effectResolver.Resolve(
            run.ActiveCombat,
            validationResult.Actor!,
            validationResult.Skill!,
            validationResult.Targets);

        var progressionLogEntries = AdvanceCombat(effectResolution.Combat, now.DateTime);

        await _runRepository.UpdateAsync(run, cancellationToken);

        var logEntries = new[] { logEntry }
            .Concat(effectResolution.LogEntries)
            .Concat(progressionLogEntries)
            .ToArray();

        return new CombatSkillActionResult(
            CombatId: effectResolution.Combat.Id.Value,
            ActorId: request.ActorId,
            SkillKey: request.SkillKey,
            TargetIds: resolvedTargetIds,
            Accepted: true,
            Message: null,
            Combat: CombatRuntimeDto.FromDomain(effectResolution.Combat),
            LogEntries: logEntries);
    }

    private static IReadOnlyCollection<CombatLogEntryDto> AdvanceCombat(
        Combat combat,
        DateTime occurredAtUtc)
    {
        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        if (combat.Status == CombatStatus.Completed)
        {
            return
            [
                CreateSystemLog(occurredAtUtc, "CombatCompleted", "Combat completed.", combat)
            ];
        }

        if (combat.Status == CombatStatus.Failed)
        {
            return
            [
                CreateSystemLog(occurredAtUtc, "CombatFailed", "Combat failed.", combat)
            ];
        }

        combat.AdvanceTurn();

        if (combat.Status == CombatStatus.Completed)
        {
            return
            [
                CreateSystemLog(occurredAtUtc, "CombatCompleted", "Combat completed.", combat)
            ];
        }

        if (combat.Status == CombatStatus.Failed)
        {
            return
            [
                CreateSystemLog(occurredAtUtc, "CombatFailed", "Combat failed.", combat)
            ];
        }

        var activeCombatant = combat.GetActiveCombatant();

        return
        [
            CreateSystemLog(
                occurredAtUtc,
                "TurnAdvanced",
                $"Turn advanced to {activeCombatant.DisplayName}.",
                combat)
        ];
    }

    private static CombatLogEntryDto CreateSystemLog(
        DateTime occurredAtUtc,
        string type,
        string message,
        Combat combat)
    {
        return new CombatLogEntryDto(
            OccurredAtUtc: occurredAtUtc,
            Type: type,
            Message: message,
            ActorId: combat.ActiveCombatantId?.Value,
            SkillKey: null,
            TargetIds: []);
    }
}
