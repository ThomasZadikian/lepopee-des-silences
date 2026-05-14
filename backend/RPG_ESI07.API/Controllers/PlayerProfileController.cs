using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Commands.PlayerProfiles;
using RPG_ESI07.Application.Queries.PlayerProfiles;
using System.Security.Claims;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlayerProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlayerProfileController(IMediator mediator) => _mediator = mediator;

    // ── GET /api/playerprofile/me ─────────────────────────────────────────────
    // Appelé par le site Vue.js pour afficher le dashboard du joueur connecté
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _mediator.Send(new GetPlayerProfileByUserIdQuery(userId));
        return Ok(result);
    }

    // ── POST /api/playerprofile ───────────────────────────────────────────────
    // Appelé par Unity après le Register pour créer le personnage
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlayerProfileCommand command)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Un joueur ne peut créer que son propre profil
        if (!User.IsInRole("Admin") && command.UserId != userId)
            return Forbid();

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetMyProfile), new { }, result);
    }

    // ── POST /api/playerprofile/sync ──────────────────────────────────────────
    // Appelé par Unity à chaque sauvegarde — met à jour tout en une seule requête
    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncGameSaveCommand command)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // On force le RequestingUserId depuis le JWT — jamais depuis le body
        var result = await _mediator.Send(command with { RequestingUserId = userId });
        return Ok(result);
    }
}