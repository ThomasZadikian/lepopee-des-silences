using Leds.Catalog.Application.RoomThemeAffinities.ListRoomThemeAffinities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/room-theme-affinities")]
public sealed class RoomThemeAffinitiesController : ControllerBase
{
    private readonly ISender _sender;
    public RoomThemeAffinitiesController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(ListRoomThemeAffinitiesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListRoomThemeAffinitiesResponse>> ListAll(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ListRoomThemeAffinitiesQuery(), cancellationToken));
}
