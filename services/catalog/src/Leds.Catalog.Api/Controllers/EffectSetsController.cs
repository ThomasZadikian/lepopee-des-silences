using Leds.Catalog.Application.EffectSets.GetEffectSetByKey;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/effect-sets")]
public sealed class EffectSetsController : ControllerBase
{
    private readonly ISender _sender;

    public EffectSetsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetEffectSetByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetEffectSetByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetEffectSetByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Definition is null)
        {
            return NotFound(new
            {
                title = "Effect set not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}
