using Leds.Catalog.Application.EventTemplates.GetEventTemplateByKey;
using Leds.Catalog.Application.EventTemplates.ListActiveEventTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/event-templates")]
public sealed class EventTemplatesController : ControllerBase
{
    private readonly ISender _sender;

    public EventTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveEventTemplatesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveEventTemplatesResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveEventTemplatesQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetEventTemplateByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetEventTemplateByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetEventTemplateByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Template is null)
        {
            return NotFound(new
            {
                title = "Event template not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}