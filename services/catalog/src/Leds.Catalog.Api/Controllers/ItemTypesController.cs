using Leds.Catalog.Domain.Items;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/item-types")]
public sealed class ItemTypesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ListActive() => Ok(new
    {
        ItemTypeCatalog.Version,
        Definitions = ItemTypeCatalog.All.Select(definition => new
        {
            definition.Code,
            definition.DisplayName,
            definition.Glyph,
            definition.Color
        })
    });
}
