using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Commands.CombatStatss;
using RPG_ESI07.Application.Queries.CombatStatss;
using RPG_ESI07.Domain;
using System.Security.Claims;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CombatStatsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CombatStatsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllCombatStatssQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole(Constants.RoleAdmin);
        var result = await _mediator.Send(new GetCombatStatsByIdQuery(id, currentUserId, isAdmin));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateCombatStatsCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCombatStatsCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole(Constants.RoleAdmin);

        var result = await _mediator.Send(command with { RequestingUserId = currentUserId, IsAdmin = isAdmin });
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole(Constants.RoleAdmin);

        var result = await _mediator.Send(new DeleteCombatStatsCommand(id, currentUserId, isAdmin));
        return Ok(result);
    }
}