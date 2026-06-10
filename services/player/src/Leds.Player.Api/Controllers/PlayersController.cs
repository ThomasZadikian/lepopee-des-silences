using Leds.Player.Application.Players;
using Leds.Player.Application.Players.CreatePlayerProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Player.Api.Controllers;

[ApiController]
[Route("api/v2/players")]
public sealed class PlayersController : ControllerBase
{
    private readonly ISender _sender;

    public PlayersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreatePlayerProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePlayerProfileResponse>> CreatePlayerProfile(
        [FromBody] CreatePlayerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePlayerProfileCommand(request.DisplayName);
        var response = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetPlayerProfile),
            new { playerId = response.Profile.Id },
            response);
    }

    [HttpGet("{playerId:guid}")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> GetPlayerProfile(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var query = new GetPlayerProfileByIdQuery(playerId);
        var profile = await _sender.Send(query, cancellationToken);

        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("{playerId:guid}/run-snapshot")]
    [ProducesResponseType(typeof(PlayerRunSnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerRunSnapshotResponse>> GetRunSnapshot(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var query = new GetPlayerRunSnapshotQuery(playerId);
        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }
}

public sealed record CreatePlayerProfileRequest(string DisplayName);
