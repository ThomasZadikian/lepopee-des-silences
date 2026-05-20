using MediatR;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RPG_ESI07.Application.Queries.Leaderboard;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowPublic")]
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

    [HttpGet("search")]
    [EnableCors("AllowPublic")]
    [EnableRateLimiting("leaderboard_search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2 || q.Length > 50)
            return BadRequest(new { message = "Recherche invalide." });

        var result = await _mediator.Send(new SearchLeaderboardQuery(q.Trim()));
        return Ok(result);
    }
}