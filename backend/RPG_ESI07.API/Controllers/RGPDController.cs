using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Commands.RGPD;
using RPG_ESI07.Application.Queries.RGPD;
using System.Security.Claims;
using System.Text.Json;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/rgpd")]
[Authorize]
public class RgpdController : ControllerBase
{
    private readonly IMediator _mediator;
    public RgpdController(IMediator mediator) => _mediator = mediator;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };


    [HttpGet("export")]
    public async Task<IActionResult> ExportMyData()
    {
        var userId = int.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _mediator.Send(new GetUserDataQuery(userId));
        return Ok(result);
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMyAccount([FromBody] string? reason)
    {
        var userId = int.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _mediator
        .Send(new AnonymizeUserCommand(userId, reason));
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result.Message);
    }

    [HttpGet("export/json")]
    public async Task<IActionResult> ExportMyDataAsJson()
    {
        var userId = int.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _mediator.Send(new GetUserDataQuery(userId));
        var json = JsonSerializer.Serialize(result, _jsonOptions);
        var fileName = $"mes-donnees-rgpd-{userId}-{DateTime.UtcNow:yyyyMMdd}.json";
        return File(
        System.Text.Encoding.UTF8.GetBytes(json),
        "application/json",
        fileName);
    }
}