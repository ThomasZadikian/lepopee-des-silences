using Leds.Player.Application.Players;
using Leds.Player.Application.Players.AwardStatPoint;
using Leds.Player.Application.Players.UnlockSkill;
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
        [FromBody] AwardStatPointsRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new AwardStatPointCommand(playerId, request?.Amount ?? 1);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/skills/{skillKey}/unlock")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> UnlockSkill(
        Guid playerId,
        Guid characterId,
        string skillKey,
        CancellationToken cancellationToken)
    {
        var command = new UnlockSkillCommand(playerId, characterId, skillKey, "devtools");
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}

public sealed record AwardStatPointsRequest(int Amount);
