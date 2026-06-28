using Leds.Catalog.Application.RewardCursePools.ListActiveRewardCursePools;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/reward-curse-pools")]
public sealed class RewardCursePoolsController : ControllerBase
{
    private readonly ISender _sender;

    public RewardCursePoolsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveRewardCursePoolsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveRewardCursePoolsResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveRewardCursePoolsQuery(),
            cancellationToken);

        return Ok(response);
    }
}