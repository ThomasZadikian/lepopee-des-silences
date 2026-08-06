using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/emotional-registers")]
public sealed class EmotionalRegistersController : ControllerBase
{
    private readonly ISender _sender;

    public EmotionalRegistersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CatalogEmotionalRegisterCatalog), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogEmotionalRegisterCatalog>> ListActive(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetEmotionalRegisterCatalogQuery(), cancellationToken));
}
