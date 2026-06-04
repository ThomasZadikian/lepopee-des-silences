using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RPG_ESI07.Application.Commands.Auth;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    => _mediator = mediator;

    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register(
    [FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(
    [FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.success)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("mfa/setup")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> SetupMfa([FromBody] SetupMfaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("mfa/verify")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.success)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("mfa")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> VerifyMfaLogin([FromBody] LoginMfaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.success)
            return Unauthorized(result);
        return Ok(result);
    }
}