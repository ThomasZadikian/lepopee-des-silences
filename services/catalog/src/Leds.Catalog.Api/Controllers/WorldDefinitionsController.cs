using Leds.Catalog.Application.Worlds.ListActiveWorldDefinitions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/worlds")]
public sealed class WorldDefinitionsController : ControllerBase
{
    private readonly ISender _sender;
    public WorldDefinitionsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveWorldDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveWorldDefinitionsResponse>> ListActive(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ListActiveWorldDefinitionsQuery(), cancellationToken));
}
