using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/health")]
public sealed class CatalogHealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            service = "catalog",
            status = "Healthy",
            version = "alpha-0.0.4"
        });
    }
}