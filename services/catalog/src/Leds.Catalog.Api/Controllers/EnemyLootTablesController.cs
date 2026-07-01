using Leds.Catalog.Application.Enemies.Loot.GetEnemyLootTableByKey;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/enemy-loot-tables")]
public sealed class EnemyLootTablesController : ControllerBase
{
    private readonly ISender _sender;

    public EnemyLootTablesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{enemyKey}")]
    [ProducesResponseType(typeof(GetEnemyLootTableByKeyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetEnemyLootTableByKeyResponse>> GetByEnemyKey(
        string enemyKey,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetEnemyLootTableByKeyQuery(enemyKey),
            cancellationToken);

        return Ok(response);
    }
}
