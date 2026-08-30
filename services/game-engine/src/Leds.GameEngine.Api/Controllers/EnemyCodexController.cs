using Leds.GameEngine.Application.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/enemy-codex")]
public sealed class EnemyCodexController : ControllerBase
{
    private readonly ISender _sender;

    public EnemyCodexController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("bosses")]
    [ProducesResponseType(typeof(ListBossCodexResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListBossCodexResponse>> ListBosses(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ListBossCodexQuery(), cancellationToken));
}
