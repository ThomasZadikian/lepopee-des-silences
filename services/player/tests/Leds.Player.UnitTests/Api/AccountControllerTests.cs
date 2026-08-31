using System.Security.Claims;
using FluentAssertions;
using Leds.Player.Api.Controllers;
using Leds.Player.Application.Accounts;
using Leds.Player.Application.Players;
using Leds.Player.Domain.Players;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Leds.Player.UnitTests.Api;

public sealed class AccountControllerTests
{
    private static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherSessionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CharacterId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Register_ShouldReturnCreatedAndForwardAllFields()
    {
        var sender = new Mock<ISender>();
        var expected = new RegisterAccountResponse(AccountId, "player@example.com", true);
        sender.Setup(x => x.Send(It.IsAny<RegisterAccountCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(sender);

        var action = await controller.Register(
            new RegisterAccountRequest("Nocturne", "player@example.com", "a-long-password", true),
            CancellationToken.None);

        var result = action.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Value.Should().Be(expected);
        sender.Verify(x => x.Send(
            It.Is<RegisterAccountCommand>(c =>
                c.DisplayName == "Nocturne"
                && c.Email == "player@example.com"
                && c.Password == "a-long-password"
                && c.AgeConfirmed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturnOk()
    {
        var sender = new Mock<ISender>();
        var expected = new VerifyEmailResponse(AccountId, true);
        sender.Setup(x => x.Send(It.IsAny<VerifyEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(sender);

        var action = await controller.VerifyEmail(new VerifyEmailRequest("verification"), CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expected);
    }

    [Fact]
    public async Task Login_ShouldReturnChallengeStatus()
    {
        var sender = new Mock<ISender>();
        var expected = new BeginLoginResponse("mfa-required", "challenge");
        sender.Setup(x => x.Send(It.IsAny<BeginLoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(sender);

        var action = await controller.Login(
            new LoginRequest("player@example.com", "a-long-password"),
            CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expected);
    }

    [Fact]
    public async Task BeginMfaEnrollment_ShouldNeverExposeProtectedSecret()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<BeginMfaEnrollmentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaEnrollmentResponse(
                "challenge",
                "protected-secret-that-must-stay-server-side",
                "otpauth://totp/LEDS",
                "MANUALKEY"));
        var controller = CreateController(sender);

        var action = await controller.BeginMfaEnrollment(
            new MfaChallengeRequest("challenge"),
            CancellationToken.None);

        var payload = action.Result.Should().BeOfType<OkObjectResult>().Which.Value
            .Should().BeOfType<MfaEnrollmentHttpResponse>().Subject;
        payload.ChallengeToken.Should().Be("challenge");
        payload.OtpAuthUri.Should().Be("otpauth://totp/LEDS");
        payload.ManualEntryKey.Should().Be("MANUALKEY");
    }

    [Theory]
    [InlineData("confirm")]
    [InlineData("challenge")]
    [InlineData("recovery")]
    public async Task SessionCreatingMfaEndpoints_ShouldSetRefreshCookie(string scenario)
    {
        var sender = new Mock<ISender>();
        var session = AuthenticatedSession(recoveryCodes: scenario == "confirm" ? ["RECOVERY-CODE"] : null);
        sender.Setup(x => x.Send(It.IsAny<ConfirmMfaEnrollmentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sender.Setup(x => x.Send(It.IsAny<CompleteMfaChallengeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sender.Setup(x => x.Send(It.IsAny<CompleteMfaRecoveryCodeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var controller = CreateController(sender, development: true);

        ActionResult<AuthenticatedSessionHttpResponse> action = scenario switch
        {
            "confirm" => await controller.ConfirmMfaEnrollment(
                new ConfirmMfaEnrollmentRequest("challenge", "123456"), CancellationToken.None),
            "challenge" => await controller.CompleteMfaChallenge(
                new CompleteMfaChallengeRequest("challenge", "123456"), CancellationToken.None),
            _ => await controller.CompleteMfaRecovery(
                new CompleteMfaRecoveryRequest("challenge", "RECOVERY-CODE"), CancellationToken.None)
        };

        var payload = action.Result.Should().BeOfType<OkObjectResult>().Which.Value
            .Should().BeOfType<AuthenticatedSessionHttpResponse>().Subject;
        payload.AccountId.Should().Be(AccountId);
        payload.SessionId.Should().Be(SessionId);
        payload.AccessToken.Should().Be("access-token");
        controller.Response.Headers.SetCookie.ToString().Should()
            .Contain("leds_refresh=")
            .And.Contain(SessionId.ToString("D"))
            .And.Contain("HttpOnly")
            .And.Contain("SameSite=Strict");
    }

    [Fact]
    public async Task SessionCookie_ShouldBeSecureOutsideDevelopment()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<CompleteMfaChallengeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticatedSession());
        var controller = CreateController(sender, development: false);

        await controller.CompleteMfaChallenge(
            new CompleteMfaChallengeRequest("challenge", "123456"),
            CancellationToken.None);

        controller.Response.Headers.SetCookie.ToString().Should().Contain("secure", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-session-cookie")]
    [InlineData(".token-that-is-long-enough")]
    [InlineData("22222222-2222-2222-2222-222222222222.")]
    [InlineData("not-a-guid.token-that-is-long-enough")]
    [InlineData("22222222-2222-2222-2222-222222222222.short")]
    public async Task Refresh_ShouldRejectMalformedOrMissingCookie(string? cookie)
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, cookie: cookie);

        var action = await controller.Refresh(CancellationToken.None);

        action.Result.Should().BeOfType<UnauthorizedResult>();
        sender.Verify(x => x.Send(It.IsAny<RefreshSessionCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_ShouldRotateValidCookieAndReturnSession()
    {
        var sender = new Mock<ISender>();
        var expected = AuthenticatedSession();
        sender.Setup(x => x.Send(It.IsAny<RefreshSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var rawRefresh = "refresh-token-that-is-long-enough";
        var controller = CreateController(sender, cookie: $"{SessionId:D}.{rawRefresh}");

        var action = await controller.Refresh(CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>();
        sender.Verify(x => x.Send(
            It.Is<RefreshSessionCommand>(c => c.SessionId == SessionId && c.RefreshToken == rawRefresh),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldReleaseGameLeaseRevokeSessionAndDeleteCookie()
    {
        var sender = SenderAcceptingVoidCommands();
        var controller = CreateController(sender, authenticated: true);

        var result = await controller.Logout(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        sender.Verify(x => x.Send(
            It.Is<ReleaseGameSessionCommand>(c => c.AccountId == AccountId && c.SessionId == SessionId),
            It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(x => x.Send(
            It.Is<LogoutSessionCommand>(c => c.AccountId == AccountId && c.SessionId == SessionId),
            It.IsAny<CancellationToken>()), Times.Once);
        controller.Response.Headers.SetCookie.ToString().Should().Contain("leds_refresh=");
    }

    [Fact]
    public async Task PasswordRecovery_ShouldReturnAccepted()
    {
        var sender = SenderAcceptingVoidCommands();
        var controller = CreateController(sender);

        var result = await controller.RequestPasswordReset(
            new PasswordRecoveryRequest("player@example.com"), CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        sender.Verify(x => x.Send(
            It.Is<RequestPasswordResetCommand>(c => c.Email == "player@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PasswordReset_ShouldDeleteRefreshCookie()
    {
        var sender = SenderAcceptingVoidCommands();
        var controller = CreateController(sender);

        var result = await controller.ResetPassword(
            new PasswordResetRequest("reset-token", "new-long-password"),
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        controller.Response.Headers.SetCookie.ToString().Should().Contain("leds_refresh=");
    }

    [Fact]
    public async Task Me_ShouldUseAuthenticatedAccountId()
    {
        var sender = new Mock<ISender>();
        var profile = PlayerProfile.Create("Nocturne", Now);
        var expected = new AccountOverviewResponse(
            AccountId,
            "Nocturne",
            "player@example.com",
            "Player",
            true,
            true,
            [],
            MainStoryProgressDto.FromDomain(profile.MainStoryProgress));
        sender.Setup(x => x.Send(It.IsAny<GetAccountOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(sender, authenticated: true);

        var action = await controller.Me(CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expected);
        sender.Verify(x => x.Send(
            It.Is<GetAccountOverviewQuery>(q => q.AccountId == AccountId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Me_ShouldRejectMissingSubjectClaim()
    {
        var controller = CreateController(new Mock<ISender>());

        var act = () => controller.Me(CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Sessions_ShouldMarkCurrentSessionThroughQuery()
    {
        var sender = new Mock<ISender>();
        IReadOnlyCollection<AccountSessionResponse> expected =
        [
            new AccountSessionResponse(SessionId, Now, Now.AddDays(1), null, null, true, true),
            new AccountSessionResponse(OtherSessionId, Now, Now.AddDays(1), null, null, false, true)
        ];
        sender.Setup(x => x.Send(It.IsAny<ListAccountSessionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(sender, authenticated: true);

        var action = await controller.Sessions(CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RevokeSession_ShouldDeleteCookieOnlyWhenRevokingCurrentSession(bool current)
    {
        var sender = SenderAcceptingVoidCommands();
        var controller = CreateController(sender, authenticated: true);
        var target = current ? SessionId : OtherSessionId;

        var result = await controller.RevokeSession(target, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        sender.Verify(x => x.Send(
            It.Is<RevokeAccountSessionCommand>(c => c.AccountId == AccountId && c.SessionId == target),
            It.IsAny<CancellationToken>()), Times.Once);
        if (current)
            controller.Response.Headers.SetCookie.ToString().Should().Contain("leds_refresh=");
        else
            controller.Response.Headers.SetCookie.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCharacter_ShouldReturnCreatedProfile()
    {
        var sender = new Mock<ISender>();
        var profile = ProfileDto();
        sender.Setup(x => x.Send(It.IsAny<CreateAccountCharacterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var controller = CreateController(sender, authenticated: true);

        var action = await controller.CreateCharacter(
            new CreateAccountCharacterRequest("Aube", "porteuse"), CancellationToken.None);

        action.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        sender.Verify(x => x.Send(
            It.Is<CreateAccountCharacterCommand>(c =>
                c.AccountId == AccountId && c.DisplayName == "Aube" && c.ArchetypeKey == "porteuse"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveCharacter_ShouldReturnUpdatedProfile()
    {
        var sender = new Mock<ISender>();
        var profile = ProfileDto();
        sender.Setup(x => x.Send(It.IsAny<ArchiveAccountCharacterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var controller = CreateController(sender, authenticated: true);

        var action = await controller.ArchiveCharacter(CharacterId, CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(profile);
    }

    [Theory]
    [InlineData("transfer-required", true)]
    [InlineData("acquired", false)]
    [InlineData("renewed", false)]
    public async Task ClaimGameSession_ShouldMapTransferRequiredToConflict(string status, bool conflict)
    {
        var sender = new Mock<ISender>();
        var expected = new GameSessionLeaseResponse(status, SessionId, Now.AddMinutes(2));
        sender.Setup(x => x.Send(It.IsAny<ClaimGameSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(sender, authenticated: true);

        var action = await controller.ClaimGameSession(
            new ClaimGameSessionRequest(ConfirmTransfer: true), CancellationToken.None);

        if (conflict)
            action.Result.Should().BeOfType<ConflictObjectResult>().Which.Value.Should().Be(expected);
        else
            action.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expected);
    }

    [Fact]
    public async Task HeartbeatGameSession_ShouldUseAuthenticatedSession()
    {
        var sender = new Mock<ISender>();
        var expected = new GameSessionLeaseResponse("renewed", SessionId, Now.AddMinutes(2));
        sender.Setup(x => x.Send(It.IsAny<HeartbeatGameSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(sender, authenticated: true);

        var action = await controller.HeartbeatGameSession(CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expected);
    }

    [Fact]
    public async Task ReleaseGameSession_ShouldReturnNoContent()
    {
        var sender = SenderAcceptingVoidCommands();
        var controller = CreateController(sender, authenticated: true);

        var result = await controller.ReleaseGameSession(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    private static AccountController CreateController(
        Mock<ISender> sender,
        bool authenticated = false,
        bool development = true,
        bool https = false,
        string? cookie = null)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName)
            .Returns(development ? Environments.Development : Environments.Production);

        var services = new Mock<IServiceProvider>();
        services.Setup(x => x.GetService(typeof(IWebHostEnvironment))).Returns(environment.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.Object
        };
        httpContext.Request.Scheme = https ? "https" : "http";
        if (cookie is not null)
            httpContext.Request.Headers.Cookie = $"leds_refresh={cookie}";
        if (authenticated)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", AccountId.ToString("D")),
                new Claim("sid", SessionId.ToString("D"))
            ], "unit-test"));
        }

        return new AccountController(sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static Mock<ISender> SenderAcceptingVoidCommands()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<ReleaseGameSessionCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(x => x.Send(It.IsAny<LogoutSessionCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(x => x.Send(It.IsAny<RequestPasswordResetCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(x => x.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(x => x.Send(It.IsAny<RevokeAccountSessionCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return sender;
    }

    private static AuthenticatedSessionResponse AuthenticatedSession(IReadOnlyCollection<string>? recoveryCodes = null) =>
        new(
            AccountId,
            SessionId,
            "access-token",
            Now.AddMinutes(15),
            "refresh-token-that-is-long-enough",
            Now.AddDays(30),
            recoveryCodes);

    private static PlayerProfileDto ProfileDto() =>
        PlayerProfileDto.FromDomain(PlayerProfile.Create("Nocturne", Now));
}
