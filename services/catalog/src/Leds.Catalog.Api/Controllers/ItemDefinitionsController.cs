using Leds.Catalog.Application.Items.Definitions.GetItemDefinitionByKey;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/item-definitions")]
public sealed class ItemDefinitionsController : ControllerBase
{
    private readonly ISender _sender;

    public ItemDefinitionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetItemDefinitionByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetItemDefinitionByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetItemDefinitionByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Definition is null)
        {
            return NotFound(new
            {
                title = "Item definition not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}
