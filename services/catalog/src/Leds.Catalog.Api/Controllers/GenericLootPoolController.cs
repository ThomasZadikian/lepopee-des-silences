using Leds.Catalog.Application.Rewards.GenericLoot.GetActiveGenericLootPool;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/generic-loot-pool")]
public sealed class GenericLootPoolController : ControllerBase
{
    private readonly ISender _sender;

    public GenericLootPoolController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetActiveGenericLootPoolResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetActiveGenericLootPoolResponse>> GetActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetActiveGenericLootPoolQuery(),
            cancellationToken);

        return Ok(response);
    }
}
