using Leds.GameEngine.Application.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/curses")]
public sealed class CursesController : ControllerBase
{
    private readonly ISender _sender;

    public CursesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListAvailableCurseDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListAvailableCurseDefinitionsResponse>> ListAvailable(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new ListAvailableCurseDefinitionsQuery(), cancellationToken);
        return Ok(response);
    }
}
