using Leds.Catalog.Application.Enemies.GetEnemyTemplateByKey;
using Leds.Catalog.Application.Enemies.ListActiveEnemyTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/enemies")]
public sealed class EnemyTemplatesController : ControllerBase
{
    private readonly ISender _sender;

    public EnemyTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveEnemyTemplatesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveEnemyTemplatesResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveEnemyTemplatesQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetEnemyTemplateByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetEnemyTemplateByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetEnemyTemplateByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Template is null)
        {
            return NotFound(new
            {
                title = "Enemy template not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}