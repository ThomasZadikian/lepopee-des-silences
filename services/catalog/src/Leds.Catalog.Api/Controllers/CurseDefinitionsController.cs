using Leds.Catalog.Application.Curses.GetCurseDefinitionByKey;
using Leds.Catalog.Application.Curses.ListAvailableCurseDefinitions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/curses")]
public sealed class CurseDefinitionsController : ControllerBase
{
    private readonly ISender _sender;

    public CurseDefinitionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListAvailableCurseDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListAvailableCurseDefinitionsResponse>> ListAvailable(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new ListAvailableCurseDefinitionsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetCurseDefinitionByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCurseDefinitionByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetCurseDefinitionByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Definition is null)
        {
            return NotFound(new
            {
                title = "Curse definition not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}
