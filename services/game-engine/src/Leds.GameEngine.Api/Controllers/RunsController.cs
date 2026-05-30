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
            nameof(GetRunByIdPlaceholder),
            new { runId = response.Run.Id },
            response);
    }

    [HttpGet("{runId:guid}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetRunByIdPlaceholder(Guid runId)
    {
        return Ok(new { RunId = runId });
    }
}

public sealed record StartRunRequest(Guid PlayerId);