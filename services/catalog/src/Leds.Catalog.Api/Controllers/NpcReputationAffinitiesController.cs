using Leds.Catalog.Application.NpcReputationAffinities.ListNpcReputationAffinities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/npc-reputation-affinities")]
public sealed class NpcReputationAffinitiesController : ControllerBase
{
    private readonly ISender _sender;
    public NpcReputationAffinitiesController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(ListNpcReputationAffinitiesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListNpcReputationAffinitiesResponse>> ListAll(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ListNpcReputationAffinitiesQuery(), cancellationToken));
}
