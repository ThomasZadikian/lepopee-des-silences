using Leds.GameEngine.Application.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/items")]
public sealed class ItemsController : ControllerBase
{
    private readonly ISender _sender;

    public ItemsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveItemDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveItemDefinitionsResponse>> ListActive(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new ListActiveItemDefinitionsQuery(), cancellationToken);
        return Ok(response);
    }
}
