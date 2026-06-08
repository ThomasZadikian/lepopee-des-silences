using Leds.GameEngine.Application.Interlude.EnterInterlude;
using Leds.GameEngine.Application.Interlude.GetInterlude;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/runs/{runId:guid}/interlude")]
public sealed class InterludeController : ControllerBase
{
    private readonly ISender _sender;

    public InterludeController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("enter")]
    [ProducesResponseType(typeof(EnterInterludeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnterInterludeResponse>> EnterInterlude(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var command = new EnterInterludeCommand(runId);

        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetInterludeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetInterludeResponse>> GetInterlude(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var query = new GetInterludeQuery(runId);

        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }
}
