using Leds.Player.Application.Players;
using Leds.Player.Application.Players.CreatePlayableCharacter;
using Leds.Player.Application.Players.CreatePlayerProfile;
using Leds.Player.Application.Players.EquipItem;
using Leds.Player.Application.Players.EquipSkill;
using Leds.Player.Application.Players.Equipment;
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
        var response = await _sender.Send(new CreatePlayerProfileCommand(request.DisplayName), cancellationToken);

        return CreatedAtAction(
            nameof(GetPlayerProfile),
            new { playerId = response.Profile.Id },
            response);
    }

    [HttpPost("{playerId:guid}/characters")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> CreatePlayableCharacter(
        Guid playerId,
        [FromBody] CreatePlayableCharacterRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(
            new CreatePlayableCharacterCommand(playerId, request.DisplayName, request.ArchetypeKey),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetPlayerProfile),
            new { playerId },
            profile);
    }

    [HttpGet("{playerId:guid}")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> GetPlayerProfile(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(new GetPlayerProfileByIdQuery(playerId), cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("{playerId:guid}/palace-progress")]
    [ProducesResponseType(typeof(MainStoryProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MainStoryProgressDto>> GetPalaceProgress(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(new GetPlayerProfileByIdQuery(playerId), cancellationToken);
        return profile is null ? NotFound() : Ok(profile.MainStory);
    }

    [HttpGet("{playerId:guid}/run-snapshot")]
    [ProducesResponseType(typeof(PlayerRunSnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerRunSnapshotResponse>> GetRunSnapshot(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetPlayerRunSnapshotQuery(playerId), cancellationToken);
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
        var response = await _sender.Send(new EquipSkillCommand(playerId, characterId, skillKey), cancellationToken);
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
        var response = await _sender.Send(new UnequipSkillCommand(playerId, characterId, skillKey), cancellationToken);
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
        var response = await _sender.Send(new EquipItemCommand(playerId, characterId, itemKey, slot), cancellationToken);
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
        var response = await _sender.Send(new UnequipItemCommand(playerId, characterId, itemKey), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/equipment/{targetPosition}/preview/{itemInstanceId:guid}")]
    public async Task<ActionResult<EquipmentChangePlan>> PreviewEquipItem(
        Guid playerId, Guid characterId, Guid itemInstanceId, EquipmentPosition targetPosition,
        [FromBody] EquipmentResourceContext? context, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new PreviewEquipItemQuery(
            playerId, characterId, itemInstanceId, targetPosition,
            context?.CurrentVitality, context?.CurrentMana), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/equipment/{targetPosition}/equip/{itemInstanceId:guid}")]
    public async Task<ActionResult<PlayerProfileDto>> EquipItemInstance(
        Guid playerId, Guid characterId, Guid itemInstanceId, EquipmentPosition targetPosition,
        [FromBody] EquipmentResourceContext? context, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new EquipItemInstanceCommand(
            playerId, characterId, itemInstanceId, targetPosition,
            context?.CurrentVitality, context?.CurrentMana), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{playerId:guid}/characters/{characterId:guid}/equipment/unequip/{itemInstanceId:guid}")]
    public async Task<ActionResult<PlayerProfileDto>> UnequipItemInstance(
        Guid playerId, Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UnequipItemInstanceCommand(playerId, characterId, itemInstanceId), cancellationToken));
    }
}

public sealed record CreatePlayerProfileRequest(string DisplayName);
public sealed record CreatePlayableCharacterRequest(string DisplayName, string ArchetypeKey);
public sealed record EquipmentResourceContext(int? CurrentVitality = null, int? CurrentMana = null);
