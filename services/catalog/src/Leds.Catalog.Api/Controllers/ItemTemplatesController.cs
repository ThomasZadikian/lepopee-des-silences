using Leds.Catalog.Application.Items.GetItemTemplateByKey;
using Leds.Catalog.Application.Items.ListActiveItemTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/items")]
public sealed class ItemTemplatesController : ControllerBase
{
    private readonly ISender _sender;

    public ItemTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveItemTemplatesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveItemTemplatesResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveItemTemplatesQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetItemTemplateByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetItemTemplateByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetItemTemplateByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Template is null)
        {
            return NotFound(new
            {
                title = "Item template not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}