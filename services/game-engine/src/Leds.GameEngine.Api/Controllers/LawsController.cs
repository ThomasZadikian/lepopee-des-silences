using Leds.GameEngine.Application.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/laws")]
public sealed class LawsController : ControllerBase
{
    private readonly ISender _sender;

    public LawsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActivePalaceLawDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActivePalaceLawDefinitionsResponse>> ListActive(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new ListActivePalaceLawDefinitionsQuery(), cancellationToken);
        return Ok(response);
    }
}
