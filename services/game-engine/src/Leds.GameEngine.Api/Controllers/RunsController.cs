using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Runs.AbandonRun;
using Leds.GameEngine.Application.Runs.ChooseNode;
using Leds.GameEngine.Application.Runs.ConfirmPermanentItemSelection;
using Leds.GameEngine.Application.Runs.EmptyRunItemContainer;
using Leds.GameEngine.Application.Runs.ExitMidRoom;
using Leds.GameEngine.Application.Runs.GetCurrentCombat;
using Leds.GameEngine.Application.Runs.GetPermanentItemCandidates;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.GetRunInventory;
using Leds.GameEngine.Application.Runs.GetRunReputation;
using Leds.GameEngine.Application.Runs.MoveToNextRoom;
using Leds.GameEngine.Application.Runs.PourRunItemLiquid;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Application.Runs.ResumeRun;
using Leds.GameEngine.Application.Runs.SaveAndExitRun;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Application.Runs.UseRunItem;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/runs")]
public sealed class RunsController : ControllerBase
{
    private readonly ISender _sender;

    public RunsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(StartRunResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StartRunResponse>> StartRun(
        [FromBody] StartRunRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StartRunCommand(request.PlayerId);

        var response = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetRunById),
            new { runId = response.Run.Id },
            response);
    }

    [HttpGet("{runId:guid}")]
    [ProducesResponseType(typeof(GetRunByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetRunByIdResponse>> GetRunById(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetRunByIdQuery(runId);

        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{runId:guid}/current-combat")]
    [ProducesResponseType(typeof(CombatRuntimeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CombatRuntimeDto>> GetCurrentCombat(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetCurrentCombatQuery(runId);

        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/nodes/{nodeId:guid}/choose")]
    [ProducesResponseType(typeof(ChooseNodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChooseNodeResponse>> ChooseNode(
    Guid runId,
    Guid nodeId,
    CancellationToken cancellationToken)
    {
        var command = new ChooseNodeCommand(runId, nodeId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/current-event/resolve")]
    [ProducesResponseType(typeof(ResolveCurrentEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResolveCurrentEventResponse>> ResolveCurrentEvent(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var command = new ResolveCurrentEventCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/progress")]
    [ProducesResponseType(typeof(ProgressRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgressRunResponse>> ProgressRun(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new ProgressRunCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/rooms/next")]
    [ProducesResponseType(typeof(MoveToNextRoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MoveToNextRoomResponse>> MoveToNextRoom(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new MoveToNextRoomCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/exit-mid-room")]
    [ProducesResponseType(typeof(ExitMidRoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExitMidRoomResponse>> ExitMidRoom(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new ExitMidRoomCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/abandon")]
    [ProducesResponseType(typeof(AbandonRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AbandonRunResponse>> AbandonRun(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new AbandonRunCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/save-and-exit")]
    [ProducesResponseType(typeof(SaveAndExitRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaveAndExitRunResponse>> SaveAndExitRun(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new SaveAndExitRunCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/resume")]
    [ProducesResponseType(typeof(ResumeRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeRunResponse>> ResumeRun(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new ResumeRunCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/current-event/choice")]
    [ProducesResponseType(typeof(ChooseCurrentEventOptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChooseCurrentEventOptionResponse>> ChooseCurrentEventOption(
    Guid runId,
    [FromBody] ChooseCurrentEventOptionCommand request,
    CancellationToken cancellationToken)
    {
        var command = new ChooseCurrentEventOptionCommand(
            runId,
            request.ChoiceId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{runId:guid}/inventory")]
    [ProducesResponseType(typeof(GetRunInventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetRunInventoryResponse>> GetRunInventory(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetRunInventoryQuery(runId);

        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/inventory/{itemId:guid}/use")]
    [ProducesResponseType(typeof(UseRunItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UseRunItemResponse>> UseRunItem(
    Guid runId,
    Guid itemId,
    CancellationToken cancellationToken)
    {
        var command = new UseRunItemCommand(runId, itemId);
        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{runId:guid}/inventory/{containerItemId:guid}/pour")]
    [ProducesResponseType(typeof(PourRunItemLiquidResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PourRunItemLiquidResponse>> PourRunItemLiquid(
        Guid runId,
        Guid containerItemId,
        [FromBody] PourRunItemLiquidRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PourRunItemLiquidCommand(runId, containerItemId, request.LiquidItemId);
        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{runId:guid}/inventory/{containerItemId:guid}/empty")]
    [ProducesResponseType(typeof(EmptyRunItemContainerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmptyRunItemContainerResponse>> EmptyRunItemContainer(
        Guid runId,
        Guid containerItemId,
        CancellationToken cancellationToken)
    {
        var command = new EmptyRunItemContainerCommand(runId, containerItemId);
        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{runId:guid}/reputation")]
    [ProducesResponseType(typeof(GetRunReputationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetRunReputationResponse>> GetRunReputation(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetRunReputationQuery(runId);
        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{runId:guid}/permanent-item-candidates")]
    [ProducesResponseType(typeof(GetPermanentItemCandidatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetPermanentItemCandidatesResponse>> GetPermanentItemCandidates(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetPermanentItemCandidatesQuery(runId);
        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{runId:guid}/permanent-items/confirm")]
    [ProducesResponseType(typeof(ConfirmPermanentItemSelectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfirmPermanentItemSelectionResponse>> ConfirmPermanentItemSelection(
        Guid runId,
        [FromBody] ConfirmPermanentItemSelectionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmPermanentItemSelectionCommand(runId, request.ItemDefinitionKeys);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}

public sealed record StartRunRequest(Guid PlayerId);

public sealed record ConfirmPermanentItemSelectionRequest(IReadOnlyCollection<string> ItemDefinitionKeys);

public sealed record PourRunItemLiquidRequest(Guid LiquidItemId);
