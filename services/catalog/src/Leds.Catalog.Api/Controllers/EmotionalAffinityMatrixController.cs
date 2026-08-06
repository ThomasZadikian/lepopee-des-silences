using Leds.Catalog.Domain.Gameplay;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/emotional-affinity-matrix")]
public sealed class EmotionalAffinityMatrixController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCurrent()
    {
        var matrix = EmotionalAffinityMatrix.Canonical;

        return Ok(new
        {
            matrix.Version,
            Rules = matrix.Rules.Select(rule => new
            {
                AttackingRegister = rule.AttackingRegister.ToString(),
                DefendingRegister = rule.DefendingRegister.ToString(),
                Outcome = rule.Outcome.ToString()
            })
        });
    }
}
