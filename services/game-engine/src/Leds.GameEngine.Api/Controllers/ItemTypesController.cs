using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

[ApiController]
[Route("api/v2/item-types")]
public sealed class ItemTypesController : ControllerBase
{
    private readonly ISender _sender;

    public ItemTypesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CatalogItemTypeCatalog), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogItemTypeCatalog>> ListActive(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetItemTypeCatalogQuery(), cancellationToken));
}
