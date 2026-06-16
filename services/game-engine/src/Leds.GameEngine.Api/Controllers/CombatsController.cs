using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.SubmitCombatAction;
using Leds.GameEngine.Application.Runs.UseCombatSkill;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Runs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/runs/{runId:guid}/combats")]
public sealed class CombatsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICombatInstanceRepository _combatInstanceRepository;
    private readonly IRunRepository _runRepository;

    public CombatsController(
        ISender sender,
        ICombatInstanceRepository combatInstanceRepository,
        IRunRepository runRepository)
    {
        _sender = sender;
        _combatInstanceRepository = combatInstanceRepository;
        _runRepository = runRepository;
    }

    [HttpGet("{combatId:guid}")]
    [ProducesResponseType(typeof(CombatInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CombatInstanceDto>> GetCombat(
        Guid runId,
        Guid combatId,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(runId), cancellationToken);

        if (run is null)
        {
            return NotFound(new { message = $"Run with id '{runId}' was not found." });
        }

        var combat = await _combatInstanceRepository.GetByIdAsync(
            new CombatId(combatId), cancellationToken);

        if (combat is null)
        {
            return NotFound(new { message = $"Combat with id '{combatId}' was not found." });
        }

        return Ok(CombatInstanceDto.FromDomain(combat));
    }

    /// <summary>
    /// LEGACY combat action endpoint (single target, <c>ActionType</c> based).
    /// Superseded by the canonical combat flow:
    /// <c>POST .../combats/{combatId}/skill-actions</c> and
    /// <c>POST .../combats/{combatId}/item-actions</c> (skill key + multi-target),
    /// which is the flow consumed by the live game client.
    /// Kept as a compatibility facade for existing integration tests.
    /// Planned for removal in alpha-0.8.x once tests are migrated to the
    /// canonical flow. See docs/audits/alpha-0.7-stabilization-audit-remediation.md.
    /// </summary>
    [Obsolete("Legacy combat path. Use /skill-actions and /item-actions. Removal targeted for alpha-0.8.x.")]
    [HttpPost("{combatId:guid}/actions")]
    [ProducesResponseType(typeof(SubmitCombatActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmitCombatActionResponse>> SubmitAction(
        Guid runId,
        Guid combatId,
        [FromBody] SubmitCombatActionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitCombatActionCommand(
            runId,
            combatId,
            request.ActorId,
            request.TargetId,
            request.ActionType);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{combatId:guid}/skill-actions")]
    [ProducesResponseType(typeof(CombatSkillActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CombatSkillActionResult>> UseCombatSkill(
        Guid runId,
        Guid combatId,
        [FromBody] UseCombatSkillRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UseCombatSkillCommand(
            RunId: runId,
            CombatId: combatId,
            ActorId: request.ActorId,
            SkillKey: request.SkillKey,
            TargetIds: request.TargetIds);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}

public sealed record UseCombatSkillRequest(
    Guid ActorId,
    string SkillKey,
    IReadOnlyCollection<Guid> TargetIds);

public sealed record SubmitCombatActionRequest(
    Guid ActorId,
    Guid TargetId,
    string ActionType);