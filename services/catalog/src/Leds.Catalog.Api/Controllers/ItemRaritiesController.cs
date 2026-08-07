using Leds.Catalog.Domain.Items;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/item-rarities")]
public sealed class ItemRaritiesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ListActive() => Ok(new
    {
        ItemRarityCatalog.Version,
        Definitions = ItemRarityCatalog.All.Select(definition => new
        {
            definition.Code,
            definition.DisplayName,
            definition.Glyph,
            definition.Color,
            definition.PalaceShardCost,
            definition.HimLitShardCost
        })
    });
}
