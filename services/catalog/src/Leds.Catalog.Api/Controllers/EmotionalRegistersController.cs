using Leds.Catalog.Domain.Gameplay;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/emotional-registers")]
public sealed class EmotionalRegistersController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ListActive()
    {
        var matrix = EmotionalAffinityMatrix.Canonical;
        return Ok(new
        {
            EmotionalRegisterCatalog.Version,
            Definitions = EmotionalRegisterCatalog.Active.Select(definition => new
            {
                definition.Code,
                definition.DisplayName,
                definition.Glyph,
                definition.Color,
                IncomingAffinities = matrix.Rules
                    .Where(rule => rule.DefendingRegister == definition.Value)
                    .Select(rule => new
                    {
                        IncomingRegister = EmotionalRegisterCatalog.CodeOf(rule.AttackingRegister),
                        Outcome = rule.Outcome.ToString(),
                        rule.Multiplier
                    })
            })
        });
    }
}
