using Leds.Catalog.Application.RoomTypes.ListActiveRoomTypeDefinitions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/room-type-definitions")]
public sealed class RoomTypeDefinitionsController : ControllerBase
{
    private readonly ISender _sender;

    public RoomTypeDefinitionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListActiveRoomTypeDefinitionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListActiveRoomTypeDefinitionsResponse>> ListActive(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ListActiveRoomTypeDefinitionsQuery(),
            cancellationToken);

        return Ok(response);
    }
}
