using Leds.Player.Application.Internal.ConsumeRunOutcome;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Player.Api.Controllers;

[ApiController]
[Route("api/v2/internal/projections")]
public sealed class InternalProjectionsController : ControllerBase
{
    private readonly ISender _sender;

    public InternalProjectionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("run-outcomes")]
    [ProducesResponseType(typeof(ConsumeRunOutcomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsumeRunOutcomeResponse>> ConsumeRunOutcome(
        [FromBody] RunOutcomeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConsumeRunOutcomeCommand(
            request.EventId,
            request.Type,
            request.EventVersion,
            request.OccurredAtUtc,
            request.PayloadJson);

        var response = await _sender.Send(command, cancellationToken);

        if (!response.Processed)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}

public sealed record RunOutcomeRequest(
    Guid EventId,
    string Type,
    string EventVersion,
    DateTime OccurredAtUtc,
    string PayloadJson);
