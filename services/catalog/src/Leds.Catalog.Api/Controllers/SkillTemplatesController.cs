using Leds.Catalog.Application.Skills.GetSkillTemplateByKey;
using Leds.Catalog.Application.Skills.ListActiveSkillTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/skills")]
public sealed class SkillTemplatesController : ControllerBase
{
    private readonly ISender _sender;

    public SkillTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveSkillTemplatesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveSkillTemplatesResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveSkillTemplatesQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetSkillTemplateByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetSkillTemplateByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetSkillTemplateByKeyQuery(key?? string.Empty),
            cancellationToken);

        if (response.Template is null)
        {
            return NotFound(new
            {
                title = "Skill template not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}