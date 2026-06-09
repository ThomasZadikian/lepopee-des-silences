using Leds.Catalog.Application.Enemies.Definitions.GetEnemyDefinitionByKey;
using Leds.Catalog.Application.Enemies.Definitions.ListActiveEnemyDefinitions;
using Leds.Catalog.Application.Enemies.Definitions.ListCompatibleEnemyDefinitions;
using Leds.Catalog.Application.Enemies.Definitions.ListEnemyDefinitionsByRoomType;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/enemy-definitions")]
public sealed class EnemyDefinitionsController : ControllerBase
{
    private readonly ISender _sender;

    public EnemyDefinitionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveEnemyDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveEnemyDefinitionsResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveEnemyDefinitionsQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetEnemyDefinitionByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetEnemyDefinitionByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetEnemyDefinitionByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Definition is null)
        {
            return NotFound(new
            {
                title = "Enemy definition not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }

    [HttpGet("room-type/{roomType}")]
    [ProducesResponseType(typeof(ListEnemyDefinitionsByRoomTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListEnemyDefinitionsByRoomTypeResponse>> GetByRoomType(
        string? roomType,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListEnemyDefinitionsByRoomTypeQuery(roomType ?? string.Empty),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("compatible")]
    [ProducesResponseType(typeof(ListCompatibleEnemyDefinitionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListCompatibleEnemyDefinitionsResponse>> GetCompatible(
        [FromQuery] string? roomType,
        [FromQuery] int? riskLevel,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListCompatibleEnemyDefinitionsQuery(
                roomType ?? string.Empty,
                riskLevel ?? 0),
            cancellationToken);

        return Ok(response);
    }
}
