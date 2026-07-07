using Leds.GameEngine.Application.Players;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/players")]
public sealed class PlayerProgressionController : ControllerBase
{
    private readonly ISender _sender;

    public PlayerProgressionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{playerId:guid}/profile")]
    [ProducesResponseType(typeof(PlayerProfileView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileView>> GetProfile(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var query = new GetPlayerProfileQuery(playerId);
        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/skills/{skillKey}/equip")]
    [ProducesResponseType(typeof(PlayerProfileView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileView>> EquipSkill(
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
    [ProducesResponseType(typeof(PlayerProfileView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileView>> UnequipSkill(
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
    [ProducesResponseType(typeof(PlayerProfileView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileView>> EquipItem(
        Guid playerId,
        Guid characterId,
        string itemKey,
        CancellationToken cancellationToken)
    {
        var command = new EquipItemCommand(playerId, characterId, itemKey);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/items/{itemKey}/unequip")]
    [ProducesResponseType(typeof(PlayerProfileView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileView>> UnequipItem(
        Guid playerId,
        Guid characterId,
        string itemKey,
        CancellationToken cancellationToken)
    {
        var command = new UnequipItemCommand(playerId, characterId, itemKey);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/stats/{stat}/spend-point")]
    [ProducesResponseType(typeof(PlayerProfileView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileView>> SpendStatPoint(
        Guid playerId,
        Guid characterId,
        string stat,
        CancellationToken cancellationToken)
    {
        var command = new SpendStatPointCommand(playerId, characterId, stat);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}
