using Leds.GameEngine.Application.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/skills")]
public sealed class SkillsController : ControllerBase
{
    private readonly ISender _sender;

    public SkillsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveSkillDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveSkillDefinitionsResponse>> ListActive(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new ListActiveSkillDefinitionsQuery(), cancellationToken);
        return Ok(response);
    }
}
