using Leds.Catalog.Application.Npcs.Definitions.ListActiveNpcDefinitions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/npc-definitions")]
public sealed class NpcDefinitionsController : ControllerBase
{
    private readonly ISender _sender;

    public NpcDefinitionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveNpcDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveNpcDefinitionsResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveNpcDefinitionsQuery(),
            cancellationToken);

        return Ok(response);
    }
}
