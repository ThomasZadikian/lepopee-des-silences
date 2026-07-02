using Leds.Player.Application.Players;
using Leds.Player.Application.Players.AwardStatPoint;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Player.Api.Controllers;

[ApiController]
[Route("api/v2/internal/players")]
public sealed class InternalPlayersController : ControllerBase
{
    private readonly ISender _sender;

    public InternalPlayersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{playerId:guid}/stat-points/award")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> AwardStatPoint(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var command = new AwardStatPointCommand(playerId);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}
