using Leds.Catalog.Application.Rooms.ListActiveRoomDefinitions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/room-definitions")]
public sealed class RoomDefinitionsController : ControllerBase
{
    private readonly ISender _sender;
    public RoomDefinitionsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveRoomDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveRoomDefinitionsResponse>> ListActive(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ListActiveRoomDefinitionsQuery(), cancellationToken));
}