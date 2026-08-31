using System.Security.Claims;
using Leds.Player.Application.Accounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Leds.Player.Api.Controllers;

[ApiController]
[Route("api/v2/account")]
public sealed class AccountController : ControllerBase
{
    private const string RefreshCookieName = "leds_refresh";
    private readonly ISender _sender;

    public AccountController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterAccountResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterAccountResponse>> Register(
        RegisterAccountRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new RegisterAccountCommand(
                request.DisplayName,
                request.Email,
                request.Password,
                request.AgeConfirmed),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmail(
        VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new VerifyEmailCommand(request.Token), cancellationToken));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<ActionResult<BeginLoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new BeginLoginCommand(request.Email, request.Password),
            cancellationToken));
    }

    [HttpPost("mfa/enrollment")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<ActionResult<MfaEnrollmentResponse>> BeginMfaEnrollment(
        MfaChallengeRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new BeginMfaEnrollmentCommand(request.ChallengeToken),
            cancellationToken));
    }

    [HttpPost("mfa/confirm")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatedSessionHttpResponse>> ConfirmMfaEnrollment(
        ConfirmMfaEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ConfirmMfaEnrollmentCommand(
                request.ChallengeToken,
                request.ProtectedSecret,
                request.Code),
            cancellationToken);
        return Ok(ToHttpSession(response));
    }

    [HttpPost("mfa/challenge")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatedSessionHttpResponse>> CompleteMfaChallenge(
        CompleteMfaChallengeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new CompleteMfaChallengeCommand(request.ChallengeToken, request.Code),
            cancellationToken);
        return Ok(ToHttpSession(response));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatedSessionHttpResponse>> Refresh(
        CancellationToken cancellationToken)
    {
        if (!TryReadRefreshCookie(out var sessionId, out var rawToken))
            return Unauthorized();

        var response = await _sender.Send(
            new RefreshSessionCommand(sessionId, rawToken),
            cancellationToken);
        return Ok(ToHttpSession(response));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var accountId = GetRequiredGuidClaim("sub");
        var sessionId = GetRequiredGuidClaim("sid");
        await _sender.Send(new LogoutSessionCommand(accountId, sessionId), cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/v2/account" });
        return NoContent();
    }

    [HttpPost("password-recovery")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestPasswordReset(
        PasswordRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RequestPasswordResetCommand(request.Email), cancellationToken);
        return Accepted();
    }

    [HttpPost("password-reset")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        PasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new ResetPasswordCommand(request.Token, request.NewPassword),
            cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/v2/account" });
        return NoContent();
    }

    private AuthenticatedSessionHttpResponse ToHttpSession(AuthenticatedSessionResponse response)
    {
        var cookieValue = $"{response.SessionId:D}.{response.RefreshToken}";
        Response.Cookies.Append(
            RefreshCookieName,
            cookieValue,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v2/account",
                Expires = response.RefreshTokenExpiresAtUtc
            });

        return new AuthenticatedSessionHttpResponse(
            response.AccountId,
            response.SessionId,
            response.AccessToken,
            response.AccessTokenExpiresAtUtc);
    }

    private bool TryReadRefreshCookie(out Guid sessionId, out string rawToken)
    {
        sessionId = Guid.Empty;
        rawToken = string.Empty;
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var cookie)
            || string.IsNullOrWhiteSpace(cookie))
        {
            return false;
        }

        var separator = cookie.IndexOf('.');
        if (separator <= 0 || separator >= cookie.Length - 1)
            return false;

        if (!Guid.TryParse(cookie[..separator], out sessionId))
            return false;

        rawToken = cookie[(separator + 1)..];
        return rawToken.Length >= 20;
    }

    private Guid GetRequiredGuidClaim(string claimType)
    {
        var value = User.FindFirstValue(claimType);
        if (!Guid.TryParse(value, out var id))
            throw new UnauthorizedAccessException($"Required authentication claim '{claimType}' is missing.");
        return id;
    }
}

public sealed record RegisterAccountRequest(
    string DisplayName,
    string Email,
    string Password,
    bool AgeConfirmed);
public sealed record VerifyEmailRequest(string Token);
public sealed record LoginRequest(string Email, string Password);
public sealed record MfaChallengeRequest(string ChallengeToken);
public sealed record ConfirmMfaEnrollmentRequest(string ChallengeToken, string ProtectedSecret, string Code);
public sealed record CompleteMfaChallengeRequest(string ChallengeToken, string Code);
public sealed record PasswordRecoveryRequest(string Email);
public sealed record PasswordResetRequest(string Token, string NewPassword);
public sealed record AuthenticatedSessionHttpResponse(
    Guid AccountId,
    Guid SessionId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc);
