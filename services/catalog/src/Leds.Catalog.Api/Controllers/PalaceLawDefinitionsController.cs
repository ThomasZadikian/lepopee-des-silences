using Leds.Catalog.Application.PalaceLaws.GetPalaceLawDefinitionByKey;
using Leds.Catalog.Application.PalaceLaws.ListActivePalaceLawDefinitions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/palace-laws")]
public sealed class PalaceLawDefinitionsController : ControllerBase
{
    private readonly ISender _sender;

    public PalaceLawDefinitionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActivePalaceLawDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActivePalaceLawDefinitionsResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActivePalaceLawDefinitionsQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetPalaceLawDefinitionByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetPalaceLawDefinitionByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetPalaceLawDefinitionByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Definition is null)
        {
            return NotFound(new
            {
                title = "Palace law definition not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}