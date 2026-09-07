using Leds.Catalog.Application.Archetypes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/archetype-definitions")]
public sealed class ArchetypeDefinitionsController : ControllerBase
{
    private readonly ISender _sender;
    public ArchetypeDefinitionsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<ListArchetypeDefinitionsResponse>> List(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ListArchetypeDefinitionsQuery(), cancellationToken));

    [HttpGet("{key}")]
    public async Task<ActionResult<GetArchetypeDefinitionResponse>> Get(string key, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetArchetypeDefinitionQuery(key), cancellationToken);
        return response.Definition is null ? NotFound() : Ok(response);
    }
}
