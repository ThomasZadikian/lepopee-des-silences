using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.SubmitCombatAction;
using Leds.GameEngine.Application.Runs.AdvanceCombatTurn;
using Leds.GameEngine.Application.Runs.HoldCombatTurn;
using Leds.GameEngine.Application.Runs.Reposition;
using Leds.GameEngine.Application.Runs.UseCombatSkill;
using Leds.GameEngine.Application.Runs.UseItemInCombat;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Runs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Leds.GameEngine.Application.Runs.HoldCombatTurn;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/runs/{runId:guid}/combats")]
public sealed class CombatsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IRunRepository _runRepository;

    public CombatsController(
        ISender sender,
        IRunRepository runRepository)
    {
        _sender = sender;
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

        var requestedCombatId = new CombatId(combatId);

        if (run.ActiveCombat is null || run.ActiveCombat.Id != requestedCombatId)
        {
            return NotFound(new { message = $"Combat with id '{combatId}' was not found." });
        }

        return Ok(CombatInstanceDto.FromDomain(run.ActiveCombat));
    }

    /// <summary>
    /// Advances the ATB clock by one turn. If an enemy is up it resolves that single
    /// enemy turn; if the player is up it is a no-op. The client calls this in real
    /// time as gauges fill, instead of the server cascading every enemy turn.
    /// </summary>
    [HttpPost("{combatId:guid}/advance")]
    [ProducesResponseType(typeof(CombatSkillActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CombatSkillActionResult>> AdvanceCombatTurn(
        Guid runId,
        Guid combatId,
        CancellationToken cancellationToken)
    {
        var command = new AdvanceCombatTurnCommand(runId, combatId);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// "Time flows" while the player holds: advances the ATB clock by a delta,
    /// charging the player and letting any newly-ready enemy act.
    /// </summary>
    [HttpPost("{combatId:guid}/hold")]
    [ProducesResponseType(typeof(CombatSkillActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CombatSkillActionResult>> HoldCombatTurn(
        Guid runId,
        Guid combatId,
        [FromBody] HoldCombatTurnRequest? body,
        CancellationToken cancellationToken)
    {
        var command = new HoldCombatTurnCommand(runId, combatId, body?.DeltaTicks ?? 180);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
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

    /// <summary>
    /// Switches the actor between Front and Back row. Costs the actor's whole turn,
    /// like a basic attack — see RepositionCommandHandler.
    /// </summary>
    [HttpPost("{combatId:guid}/reposition-actions")]
    [ProducesResponseType(typeof(CombatSkillActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CombatSkillActionResult>> Reposition(
        Guid runId,
        Guid combatId,
        [FromBody] RepositionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RepositionCommand(
            RunId: runId,
            CombatId: combatId,
            ActorId: request.ActorId,
            Row: request.Row);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{combatId:guid}/item-actions")]
    [ProducesResponseType(typeof(CombatSkillActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CombatSkillActionResult>> UseItemInCombat(
        Guid runId,
        Guid combatId,
        [FromBody] UseItemInCombatRequest body,
        CancellationToken cancellationToken)
    {
        var command = new UseItemInCombatCommand(
            runId, combatId, body.ItemId, body.TargetIds ?? []);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
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

public sealed record UseItemInCombatRequest(
    Guid ItemId,
    IReadOnlyCollection<Guid>? TargetIds);

public sealed record HoldCombatTurnRequest(int DeltaTicks);

public sealed record RepositionRequest(
    Guid ActorId,
    string Row);
