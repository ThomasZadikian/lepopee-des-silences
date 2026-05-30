using Leds.GameEngine.Application.Runs.ChooseNode;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.ResolveSelectedNode;
using Leds.GameEngine.Application.Runs.StartRun;
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

    [HttpPost("{runId:guid}/selected-node/resolve")]
    [ProducesResponseType(typeof(ResolveSelectedNodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResolveSelectedNodeResponse>> ResolveSelectedNode(
    Guid runId,
    CancellationToken cancellationToken)
    {
        var command = new ResolveSelectedNodeCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}

public sealed record StartRunRequest(Guid PlayerId);