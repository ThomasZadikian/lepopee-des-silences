using Leds.GameEngine.Domain.Runs;
using Microsoft.AspNetCore.Mvc;

namespace Leds.GameEngine.Api.Controllers;

// RunItemEffectType is a game-engine domain enum with no Catalog-side counterpart
// type (Catalog only ever carries the matching string in EffectRunType) — this
// catalog is static and owned directly here, no HTTP round-trip to Catalog needed.
[ApiController]
[Route("api/v2/item-effect-types")]
public sealed class ItemEffectTypesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ListActive() => Ok(new
    {
        RunItemEffectTypeCatalog.Version,
        Definitions = RunItemEffectTypeCatalog.All.Select(definition => new
        {
            definition.Code,
            definition.DisplayName,
            definition.Glyph,
            definition.Color
        })
    });
}
