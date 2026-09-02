using System.Security.Claims;
using FluentAssertions;
using Leds.Player.Api.Controllers;
using Leds.Player.Application.Accounts;
using Leds.Player.Application.Players;
using Leds.Player.Domain.Players;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Leds.Player.UnitTests.Api;

public sealed class AccountControllerTests
{
    private static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherSessionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AnonymousAccountEndpoints_ShouldMapApplicationResponses()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<RegisterAccountCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterAccountResponse(AccountId, "player@example.com", true));
        sender.Setup(x => x.Send(It.IsAny<VerifyEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyEmailResponse(AccountId, true));
        sender.Setup(x => x.Send(It.IsAny<BeginLoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BeginLoginResponse("mfa-required", "challenge"));
        sender.Setup(x => x.Send(It.IsAny<BeginMfaEnrollmentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaEnrollmentResponse("challenge", "protected", "otpauth://totp/leds", "MANUAL"));
        sender.Setup(x => x.Send(It.IsAny<RequestPasswordResetCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(x => x.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(sender);

        (await controller.Register(
            new RegisterAccountRequest("Nocturne", "player@example.com", "a-long-password", true),
            CancellationToken.None)).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        (await controller.VerifyEmail(new VerifyEmailRequest("token"), CancellationToken.None))
            .Result.Should().BeOfType<OkObjectResult>();
        (await controller.Login(new LoginRequest("player@example.com", "a-long-password"), CancellationToken.None))
            .Result.Should().BeOfType<OkObjectResult>();
        var enrollment = (await controller.BeginMfaEnrollment(
                new MfaChallengeRequest("challenge"), CancellationToken.None))
            .Result.Should().BeOfType<OkObjectResult>().Which.Value
            .Should().BeOfType<MfaEnrollmentHttpResponse>().Subject;
        enrollment.OtpAuthUri.Should().Be("otpauth://totp/leds");
        enrollment.Should().NotBeEquivalentTo(new { ProtectedSecret = "protected" });
        (await controller.RequestPasswordReset(
            new PasswordRecoveryRequest("player@example.com"), CancellationToken.None))
            .Should().BeOfType<AcceptedResult>();
        (await controller.ResetPassword(
            new PasswordResetRequest("token", "another-long-password"), CancellationToken.None))
            .Should().BeOfType<NoContentResult>();
    }

    [Theory]
    [InlineData("confirm")]
    [InlineData("challenge")]
    [InlineData("recovery")]
    public async Task MfaCompletionEndpoints_ShouldCreateHttpOnlyRefreshCookie(string endpoint)
    {
        var sender = new Mock<ISender>();
        var session = Session();
        sender.Setup(x => x.Send(It.IsAny<ConfirmMfaEnrollmentCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
        sender.Setup(x => x.Send(It.IsAny<CompleteMfaChallengeCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
        sender.Setup(x => x.Send(It.IsAny<CompleteMfaRecoveryCodeCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
        var controller = CreateController(sender);

        ActionResult<AuthenticatedSessionHttpResponse> result = endpoint switch
        {
            "confirm" => await controller.ConfirmMfaEnrollment(new("challenge", "123456"), CancellationToken.None),
            "challenge" => await controller.CompleteMfaChallenge(new("challenge", "123456"), CancellationToken.None),
            _ => await controller.CompleteMfaRecovery(new("challenge", "RECOVERY-CODE"), CancellationToken.None)
        };

        result.Result.Should().BeOfType<OkObjectResult>();
        var cookie = controller.Response.Headers.SetCookie.ToString();
        cookie.Should().Contain("leds_refresh=").And.Contain("httponly", Exactly.Once(), "cookie must be inaccessible to JavaScript");
    }

    [Fact]
    public async Task SessionCookie_ShouldBeSecureInProduction()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<CompleteMfaChallengeCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Session());
        var controller = CreateController(sender, development: false);

        await controller.CompleteMfaChallenge(new("challenge", "123456"), CancellationToken.None);

        controller.Response.Headers.SetCookie.ToString().ToLowerInvariant().Should().Contain("secure");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData(".token-that-is-long-enough")]
    [InlineData("22222222-2222-2222-2222-222222222222.")]
    [InlineData("not-a-guid.token-that-is-long-enough")]
    [InlineData("22222222-2222-2222-2222-222222222222.short")]
    public async Task Refresh_ShouldRejectEveryMalformedCookieBranch(string? cookie)
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, cookie: cookie);

        var result = await controller.Refresh(CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Refresh_ShouldForwardValidSessionCookieAndRotateIt()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<RefreshSessionCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Session());
        var rawToken = "refresh-token-that-is-long-enough";
        var controller = CreateController(sender, cookie: $"{SessionId:D}.{rawToken}");

        var result = await controller.Refresh(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        sender.Verify(x => x.Send(
            It.Is<RefreshSessionCommand>(c => c.SessionId == SessionId && c.RefreshToken == rawToken),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticatedAccountEndpoints_ShouldUseClaimsAndMapResponses()
    {
        var sender = new Mock<ISender>();
        var profile = PlayerProfileDto.FromDomain(PlayerProfile.Create("Nocturne", Now));
        var mainStory = profile.MainStory;
        sender.Setup(x => x.Send(It.IsAny<GetAccountOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountOverviewResponse(AccountId, "Nocturne", "player@example.com", "Player", true, true, [], mainStory));
        sender.Setup(x => x.Send(It.IsAny<ListAccountSessionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AccountSessionResponse(SessionId, Now, Now.AddDays(1), null, null, true, true)]);
        sender.Setup(x => x.Send(It.IsAny<CreateAccountCharacterCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        sender.Setup(x => x.Send(It.IsAny<ArchiveAccountCharacterCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        sender.Setup(x => x.Send(It.IsAny<HeartbeatGameSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameSessionLeaseResponse("renewed", SessionId, Now.AddMinutes(2)));
        SetupVoidCommands(sender);
        var controller = CreateController(sender, authenticated: true);

        (await controller.Me(CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.Sessions(CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.CreateCharacter(new("Aube", "porteur"), CancellationToken.None))
            .Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        (await controller.ArchiveCharacter(Guid.NewGuid(), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.HeartbeatGameSession(CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.ReleaseGameSession(CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.Logout(CancellationToken.None)).Should().BeOfType<NoContentResult>();
    }

    [Theory]
    [InlineData("transfer-required", true)]
    [InlineData("acquired", false)]
    public async Task ClaimGameSession_ShouldExposeTransferConflict(string status, bool conflict)
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<ClaimGameSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameSessionLeaseResponse(status, OtherSessionId, Now.AddMinutes(2)));
        var controller = CreateController(sender, authenticated: true);

        var result = await controller.ClaimGameSession(new(true), CancellationToken.None);

        if (conflict)
            result.Result.Should().BeOfType<ConflictObjectResult>();
        else
            result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RevokeSession_ShouldDeleteCookieOnlyForCurrentSession(bool current)
    {
        var sender = new Mock<ISender>();
        SetupVoidCommands(sender);
        var controller = CreateController(sender, authenticated: true);

        await controller.RevokeSession(current ? SessionId : OtherSessionId, CancellationToken.None);

        var setCookie = controller.Response.Headers.SetCookie.ToString();
        if (current)
            setCookie.Should().Contain("leds_refresh=");
        else
            setCookie.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthenticatedEndpoint_ShouldRejectMissingGuidClaim()
    {
        var controller = CreateController(new Mock<ISender>());

        var act = () => controller.Me(CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static void SetupVoidCommands(Mock<ISender> sender)
    {
        sender.Setup(x => x.Send(It.IsAny<ReleaseGameSessionCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        sender.Setup(x => x.Send(It.IsAny<LogoutSessionCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        sender.Setup(x => x.Send(It.IsAny<RevokeAccountSessionCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private static AccountController CreateController(
        Mock<ISender> sender,
        bool authenticated = false,
        bool development = true,
        string? cookie = null)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(development ? Environments.Development : Environments.Production);
        var services = new Mock<IServiceProvider>();
        services.Setup(x => x.GetService(typeof(IWebHostEnvironment))).Returns(environment.Object);
        var context = new DefaultHttpContext { RequestServices = services.Object };
        if (cookie is not null)
            context.Request.Headers.Cookie = $"leds_refresh={cookie}";
        if (authenticated)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", AccountId.ToString("D")),
                new Claim("sid", SessionId.ToString("D"))
            ], "test"));
        }

        return new AccountController(sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static AuthenticatedSessionResponse Session() =>
        new(AccountId, SessionId, "access-token", Now.AddMinutes(15), "refresh-token-that-is-long-enough", Now.AddDays(30));
}
