using Leds.Catalog.Application.RewardTemplates.GetRewardTemplateByKey;
using Leds.Catalog.Application.RewardTemplates.ListEligibleRewardTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/reward-templates")]
public sealed class RewardTemplatesController : ControllerBase
{
    private readonly ISender _sender;

    public RewardTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("eligible")]
    [ProducesResponseType(typeof(ListEligibleRewardTemplatesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListEligibleRewardTemplatesResponse>> ListEligible(
        [FromQuery] string sourceType,
        [FromQuery] int? depth,
        [FromQuery] string? combatTier,
        [FromQuery] double? difficultyMultiplier,
        [FromQuery] double? rewardPowerMultiplier,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListEligibleRewardTemplatesQuery(
                sourceType,
                depth,
                combatTier,
                difficultyMultiplier,
                rewardPowerMultiplier),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(GetRewardTemplateByKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetRewardTemplateByKeyResponse>> GetByKey(
        string? key,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetRewardTemplateByKeyQuery(key ?? string.Empty),
            cancellationToken);

        if (response.Definition is null)
        {
            return NotFound(new
            {
                title = "Reward template not found.",
                status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }
}
