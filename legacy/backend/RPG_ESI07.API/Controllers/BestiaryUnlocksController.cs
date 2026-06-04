using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Commands.BestiaryUnlocks;
using RPG_ESI07.Application.Queries.BestiaryUnlocks;
using RPG_ESI07.Application.Queries.PlayerProfiles;
using RPG_ESI07.Domain; 
using System.Security.Claims;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BestiaryUnlocksController : ControllerBase
{
    private readonly IMediator _mediator;

    public BestiaryUnlocksController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllBestiaryUnlocksQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole(Constants.RoleAdmin);
        var profile = await _mediator.Send(new GetPlayerProfileByUserIdQuery(currentUserId));
        if (profile == null) return NotFound();
        var result = await _mediator.Send(new GetBestiaryUnlockByIdQuery(id, profile.Id, isAdmin));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBestiaryUnlockCommand command)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole(Constants.RoleAdmin);
        if (!isAdmin)
        {
            var profile = await _mediator.Send(new GetPlayerProfileByUserIdQuery(currentUserId));
            if (profile == null) return NotFound();
            if (command.PlayerId != profile.Id) return Forbid();
        }

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBestiaryUnlockCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole(Constants.RoleAdmin);
        var profile = await _mediator.Send(new GetPlayerProfileByUserIdQuery(currentUserId));
        if (profile == null) return NotFound();

        var result = await _mediator.Send(command with { RequestingUserId = profile.Id, IsAdmin = isAdmin });
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole(Constants.RoleAdmin);
        var profile = await _mediator.Send(new GetPlayerProfileByUserIdQuery(currentUserId));
        if (profile == null) return NotFound();

        var result = await _mediator.Send(new DeleteBestiaryUnlockCommand(id, profile.Id, isAdmin));
        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var profile = await _mediator.Send(new GetPlayerProfileByUserIdQuery(currentUserId));
        if (profile == null) return NotFound();
        var result = await _mediator.Send(new GetAllBestiaryUnlocksQuery(profile.Id));
        return Ok(result);
    }

    [HttpPost("me")]
    public async Task<IActionResult> CreateForMe([FromBody] CreateBestiaryUnlockForMeCommand command)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var profile = await _mediator.Send(new GetPlayerProfileByUserIdQuery(currentUserId));
        if (profile == null) return NotFound("Profil introuvable.");

        var result = await _mediator.Send(new CreateBestiaryUnlockCommand(profile.Id, command.EnemyId));
        return Ok(result);
    }
}