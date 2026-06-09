using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
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
    private readonly IClock _clock;

    public UseCombatSkillCommandHandler(
        IRunRepository runRepository,
        ICombatSkillActionValidator validator,
        IClock clock)
    {
        _runRepository = runRepository;
        _validator = validator;
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

        var logEntry = new CombatLogEntryDto(
            OccurredAtUtc: now.DateTime,
            Type: "ActionAccepted",
            Message: $"Skill '{request.SkillKey}' used by actor '{request.ActorId}'.",
            ActorId: request.ActorId,
            SkillKey: request.SkillKey,
            TargetIds: request.TargetIds);

        return new CombatSkillActionResult(
            CombatId: run.ActiveCombat.Id.Value,
            ActorId: request.ActorId,
            SkillKey: request.SkillKey,
            TargetIds: request.TargetIds,
            Accepted: true,
            Message: null,
            Combat: CombatRuntimeDto.FromDomain(run.ActiveCombat),
            LogEntries: [logEntry]);
    }
}
