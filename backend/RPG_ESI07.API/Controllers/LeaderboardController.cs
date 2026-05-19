using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RPG_ESI07.Application.Queries.Leaderboard;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaderboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [EnableRateLimiting("leaderboard")]
    public async Task<IActionResult> Get([FromQuery] string sortBy = "level", [FromQuery] int page = 1)
    {
        var result = await _mediator.Send(new GetLeaderboardQuery(sortBy, page));
        return Ok(result);
    }
}