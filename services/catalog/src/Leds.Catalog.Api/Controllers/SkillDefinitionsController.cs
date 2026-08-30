using Leds.Catalog.Application.Skills.Definitions.GetSkillDefinitionByKey;
using Leds.Catalog.Application.Skills.Definitions.ListActiveSkillDefinitions;
using Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByKeys;
using Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByType;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/skill-definitions")]
public sealed class SkillDefinitionsController : ControllerBase
{
    private readonly ISender _sender;

    public SkillDefinitionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveSkillDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveSkillDefinitionsResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveSkillDefinitionsQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetSkillDefinitionByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetSkillDefinitionByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetSkillDefinitionByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Definition is null)
        {
            return NotFound(new
            {
                title = "Skill definition not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }

    [HttpGet("type/{skillType}")]
    [ProducesResponseType(typeof(ListSkillDefinitionsByTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListSkillDefinitionsByTypeResponse>> GetByType(
        string? skillType,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListSkillDefinitionsByTypeQuery(skillType ?? string.Empty),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("batch/by-keys")]
    [ProducesResponseType(typeof(ListSkillDefinitionsByKeysResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListSkillDefinitionsByKeysResponse>> GetByKeys(
        [FromBody] ListSkillDefinitionsByKeysQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(query, cancellationToken);

        return Ok(response);
    }
}
