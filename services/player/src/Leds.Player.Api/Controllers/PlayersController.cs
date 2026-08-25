using Leds.Player.Application.Players;
using Leds.Player.Application.Players.CreatePlayerProfile;
using Leds.Player.Application.Players.EquipItem;
using Leds.Player.Application.Players.EquipSkill;
using Leds.Player.Application.Players.UnequipItem;
using Leds.Player.Application.Players.UnequipSkill;
using Leds.Player.Domain.Players;
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

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/skills/{skillKey}/equip")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> EquipSkill(
        Guid playerId,
        Guid characterId,
        string skillKey,
        CancellationToken cancellationToken)
    {
        var command = new EquipSkillCommand(playerId, characterId, skillKey);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/skills/{skillKey}/unequip")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> UnequipSkill(
        Guid playerId,
        Guid characterId,
        string skillKey,
        CancellationToken cancellationToken)
    {
        var command = new UnequipSkillCommand(playerId, characterId, skillKey);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/items/{itemKey}/equip")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> EquipItem(
        Guid playerId,
        Guid characterId,
        string itemKey,
        [FromQuery] EquipmentSlotKind slot,
        CancellationToken cancellationToken)
    {
        var command = new EquipItemCommand(playerId, characterId, itemKey, slot);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/items/{itemKey}/unequip")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> UnequipItem(
        Guid playerId,
        Guid characterId,
        string itemKey,
        CancellationToken cancellationToken)
    {
        var command = new UnequipItemCommand(playerId, characterId, itemKey);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

}

public sealed record CreatePlayerProfileRequest(string DisplayName);
