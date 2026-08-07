using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/item-rarities")]
public sealed class ItemRaritiesController : ControllerBase
{
    private readonly ISender _sender;

    public ItemRaritiesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CatalogItemRarityCatalog), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogItemRarityCatalog>> ListActive(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetItemRarityCatalogQuery(), cancellationToken));
}
