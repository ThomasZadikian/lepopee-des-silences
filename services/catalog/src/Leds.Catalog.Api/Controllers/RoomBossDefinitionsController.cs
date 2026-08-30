using Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByKey;
using Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByRoomType;
using Leds.Catalog.Application.RoomBosses.ListActiveRoomBossDefinitions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/room-boss-definitions")]
public sealed class RoomBossDefinitionsController : ControllerBase
{
    private readonly ISender _sender;

    public RoomBossDefinitionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveRoomBossDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveRoomBossDefinitionsResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveRoomBossDefinitionsQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetRoomBossDefinitionByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetRoomBossDefinitionByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetRoomBossDefinitionByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Definition is null)
        {
            return NotFound(new
            {
                title = "Room boss definition not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }

    [HttpGet("room-type/{roomType}")]
    [ProducesResponseType(typeof(GetRoomBossDefinitionByRoomTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetRoomBossDefinitionByRoomTypeResponse>> GetByRoomType(
        string? roomType,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetRoomBossDefinitionByRoomTypeQuery(roomType ?? string.Empty),
            cancellationToken);

        if (response.Definition is null)
        {
            return NotFound(new
            {
                title = "Room boss definition not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}
