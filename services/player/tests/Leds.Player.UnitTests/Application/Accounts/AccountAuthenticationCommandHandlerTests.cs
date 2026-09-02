using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Accounts;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Identity;
using Leds.Player.Domain.Sessions;
using Moq;

namespace Leds.Player.UnitTests.Application.Accounts;

public sealed class AccountAuthenticationCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TokenId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SessionId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Register_ShouldPersistProfileIdentityAndSendVerificationEmail()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var emailSender = new Mock<IAccountEmailSender>();
        store.Setup(x => x.EmailExistsAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        security.Setup(x => x.HashPassword("correcthorse")).Returns("argon-hash");
        security.Setup(x => x.GenerateOpaqueToken()).Returns(new OpaqueToken("raw-verification", "hash-verification"));

        var sut = new RegisterAccountCommandHandler(store.Object, security.Object, emailSender.Object, Time());
        var result = await sut.Handle(
            new RegisterAccountCommand("Nocturne", "  PLAYER@example.COM ", "correcthorse", true),
            CancellationToken.None);

        result.Email.Should().Be("player@example.com");
        result.EmailVerificationRequired.Should().BeTrue();
        result.AccountId.Should().NotBeEmpty();
        store.Verify(x => x.RegisterAsync(
            It.Is<Leds.Player.Domain.Players.PlayerProfile>(p => p.Id.Value == result.AccountId && p.DisplayName == "Nocturne"),
            It.Is<UserIdentity>(i => i.AccountId == result.AccountId && i.PasswordHash == "argon-hash"),
            "hash-verification",
            Now.AddHours(24),
            It.IsAny<CancellationToken>()), Times.Once);
        emailSender.Verify(x => x.SendVerificationEmailAsync(
            It.Is<EmailAddress>(e => e.Value == "player@example.com"),
            "raw-verification",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_ShouldRejectDuplicateEmailBeforeHashingPassword()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        store.Setup(x => x.EmailExistsAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = new RegisterAccountCommandHandler(
            store.Object,
            security.Object,
            Mock.Of<IAccountEmailSender>(),
            Time());

        var act = () => sut.Handle(
            new RegisterAccountCommand("Nocturne", "player@example.com", "correcthorse", true),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        security.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_ShouldRejectMissingAgeConfirmation()
    {
        var sut = new RegisterAccountCommandHandler(
            Mock.Of<IAccountStore>(),
            NewSecurity().Object,
            Mock.Of<IAccountEmailSender>(),
            Time());

        var act = () => sut.Handle(
            new RegisterAccountCommand("Nocturne", "player@example.com", "correcthorse", false),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task VerifyEmail_ShouldVerifyIdentityAndConsumeToken()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var identity = Identity();
        security.Setup(x => x.HashOpaqueToken("raw-token")).Returns("token-hash");
        store.Setup(x => x.FindSecurityTokenAsync("email-verification", "token-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Token("email-verification", Now.AddMinutes(1)));
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);

        var sut = new VerifyEmailCommandHandler(store.Object, security.Object, Time());
        var result = await sut.Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None);

        result.Should().Be(new VerifyEmailResponse(AccountId, true));
        identity.IsEmailVerified.Should().BeTrue();
        store.Verify(x => x.SaveIdentityAsync(identity, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.ConsumeSecurityTokenAsync(TokenId, Now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task VerifyEmail_ShouldRejectMissingOrExpiredToken(bool returnToken, bool expired)
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        security.Setup(x => x.HashOpaqueToken("raw-token")).Returns("token-hash");
        store.Setup(x => x.FindSecurityTokenAsync("email-verification", "token-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnToken ? Token("email-verification", expired ? Now : Now.AddMinutes(1)) : null);
        var sut = new VerifyEmailCommandHandler(store.Object, security.Object, Time());

        var act = () => sut.Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task VerifyEmail_ShouldFailWhenIdentityNoLongerExists()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        security.Setup(x => x.HashOpaqueToken("raw-token")).Returns("token-hash");
        store.Setup(x => x.FindSecurityTokenAsync("email-verification", "token-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Token("email-verification", Now.AddMinutes(1)));
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentity?)null);
        var sut = new VerifyEmailCommandHandler(store.Object, security.Object, Time());

        var act = () => sut.Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Login_ShouldHideInvalidEmailBehindGenericUnauthorizedResponse()
    {
        var sut = new BeginLoginCommandHandler(Mock.Of<IAccountStore>(), NewSecurity().Object, Time());

        var act = () => sut.Handle(new BeginLoginCommand("invalid", "correcthorse"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Message.Should().Be("Invalid email or password.");
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Login_ShouldRejectUnknownIdentityOrWrongPassword(bool identityExists, bool passwordValid)
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var identity = identityExists ? Identity() : null;
        store.Setup(x => x.FindIdentityByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        security.Setup(x => x.VerifyPassword("correcthorse", It.IsAny<string>())).Returns(passwordValid);
        var sut = new BeginLoginCommandHandler(store.Object, security.Object, Time());

        var act = () => sut.Handle(
            new BeginLoginCommand("player@example.com", "correcthorse"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_ShouldRequireEmailVerificationWithoutCreatingChallenge()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        store.Setup(x => x.FindIdentityByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Identity());
        security.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var sut = new BeginLoginCommandHandler(store.Object, security.Object, Time());

        var result = await sut.Handle(
            new BeginLoginCommand("player@example.com", "correcthorse"),
            CancellationToken.None);

        result.Status.Should().Be("email-verification-required");
        result.EmailVerificationRequired.Should().BeTrue();
        security.Verify(x => x.GenerateOpaqueToken(), Times.Never);
    }

    [Theory]
    [InlineData(false, "mfa-setup-required", "mfa-setup", 10)]
    [InlineData(true, "mfa-required", "mfa-challenge", 5)]
    public async Task Login_ShouldCreateCorrectMfaChallenge(
        bool mfaConfigured,
        string expectedStatus,
        string expectedPurpose,
        int expectedMinutes)
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var identity = Identity(emailVerified: true, mfaConfigured: mfaConfigured);
        store.Setup(x => x.FindIdentityByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        security.Setup(x => x.VerifyPassword("correcthorse", identity.PasswordHash)).Returns(true);
        security.Setup(x => x.GenerateOpaqueToken()).Returns(new OpaqueToken("challenge-raw", "challenge-hash"));
        var sut = new BeginLoginCommandHandler(store.Object, security.Object, Time());

        var result = await sut.Handle(
            new BeginLoginCommand("player@example.com", "correcthorse"),
            CancellationToken.None);

        result.Status.Should().Be(expectedStatus);
        result.ChallengeToken.Should().Be("challenge-raw");
        store.Verify(x => x.StoreSecurityTokenAsync(
            AccountId,
            expectedPurpose,
            "challenge-hash",
            Now,
            Now.AddMinutes(expectedMinutes),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BeginMfaEnrollment_ShouldReturnEnrollmentForVerifiedIdentity()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var identity = Identity(emailVerified: true);
        identity.StageMfaSecret("protected");
        SetupChallenge(store, security, "mfa-setup");
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        security.Setup(x => x.CreateMfaEnrollment(identity.Email))
            .Returns(new MfaEnrollment("protected", "otpauth://uri", "MANUALKEY"));
        var sut = new BeginMfaEnrollmentCommandHandler(store.Object, security.Object, Time());

        var result = await sut.Handle(new BeginMfaEnrollmentCommand("challenge"), CancellationToken.None);

        result.ProtectedSecret.Should().Be("protected");
        result.OtpAuthUri.Should().Be("otpauth://uri");
        result.ManualEntryKey.Should().Be("MANUALKEY");
        result.ChallengeToken.Should().Be("challenge");
    }

    [Fact]
    public async Task BeginMfaEnrollment_ShouldRejectInvalidChallenge()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        security.Setup(x => x.HashOpaqueToken("challenge")).Returns("challenge-hash");
        store.Setup(x => x.FindSecurityTokenAsync("mfa-setup", "challenge-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityTokenState?)null);
        var sut = new BeginMfaEnrollmentCommandHandler(store.Object, security.Object, Time());

        var act = () => sut.Handle(new BeginMfaEnrollmentCommand("challenge"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task BeginMfaEnrollment_ShouldRejectUnverifiedIdentity()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        SetupChallenge(store, security, "mfa-setup");
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Identity());
        var sut = new BeginMfaEnrollmentCommandHandler(store.Object, security.Object, Time());

        var act = () => sut.Handle(new BeginMfaEnrollmentCommand("challenge"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task BeginMfaEnrollment_ShouldRejectAlreadyConfiguredIdentity()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        SetupChallenge(store, security, "mfa-setup");
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Identity(emailVerified: true, mfaConfigured: true));
        var sut = new BeginMfaEnrollmentCommandHandler(store.Object, security.Object, Time());

        var act = () => sut.Handle(new BeginMfaEnrollmentCommand("challenge"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ConfirmMfaEnrollment_ShouldConfigureMfaConsumeChallengeAndCreateSession()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var issuer = NewIssuer();
        var identity = Identity(emailVerified: true);
        SetupChallenge(store, security, "mfa-setup");
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        security.Setup(x => x.VerifyTotp("protected", "123456", Now)).Returns(true);
        security.Setup(x => x.GenerateOpaqueToken()).Returns(new OpaqueToken("refresh-raw", "refresh-hash"));
        issuer.Setup(x => x.Issue(identity, It.IsAny<Guid>(), Now, TimeSpan.FromMinutes(15)))
            .Returns((UserIdentity _, Guid _, DateTimeOffset _, TimeSpan _) => new AccessTokenResult("access", Now.AddMinutes(15)));
        var sut = new ConfirmMfaEnrollmentCommandHandler(store.Object, security.Object, issuer.Object, Time());

        var result = await sut.Handle(
            new ConfirmMfaEnrollmentCommand("challenge", "123456"),
            CancellationToken.None);

        identity.IsMfaConfigured.Should().BeTrue();
        result.AccountId.Should().Be(AccountId);
        result.RefreshToken.Should().Be("refresh-raw");
        result.AccessToken.Should().Be("access");
        result.RefreshTokenExpiresAtUtc.Should().Be(Now.AddDays(30));
        store.Verify(x => x.ConsumeSecurityTokenAsync(TokenId, Now, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.AddSessionAsync(
            It.Is<AccountSession>(s => s.AccountId == AccountId && s.RefreshTokenHash == "refresh-hash"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmMfaEnrollment_ShouldRejectInvalidTotpWithoutMutatingIdentity()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        SetupChallenge(store, security, "mfa-setup");
        var identity = Identity(emailVerified: true);
        identity.StageMfaSecret("protected");
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        security.Setup(x => x.VerifyTotp("protected", "000000", Now)).Returns(false);
        var sut = new ConfirmMfaEnrollmentCommandHandler(store.Object, security.Object, NewIssuer().Object, Time());

        var act = () => sut.Handle(
            new ConfirmMfaEnrollmentCommand("challenge", "000000"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        identity.IsMfaConfigured.Should().BeFalse();
        store.Verify(x => x.SaveIdentityAsync(It.IsAny<UserIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteMfaChallenge_ShouldCreateSessionForValidCode()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var issuer = NewIssuer();
        var identity = Identity(emailVerified: true, mfaConfigured: true);
        SetupChallenge(store, security, "mfa-challenge");
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        security.Setup(x => x.VerifyTotp("protected-totp", "654321", Now)).Returns(true);
        security.Setup(x => x.GenerateOpaqueToken()).Returns(new OpaqueToken("refresh-raw", "refresh-hash"));
        issuer.Setup(x => x.Issue(identity, It.IsAny<Guid>(), Now, TimeSpan.FromMinutes(15)))
            .Returns(new AccessTokenResult("access", Now.AddMinutes(15)));
        var sut = new CompleteMfaChallengeCommandHandler(store.Object, security.Object, issuer.Object, Time());

        var result = await sut.Handle(
            new CompleteMfaChallengeCommand("challenge", "654321"),
            CancellationToken.None);

        result.AccessToken.Should().Be("access");
        store.Verify(x => x.ConsumeSecurityTokenAsync(TokenId, Now, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.AddSessionAsync(It.IsAny<AccountSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompleteMfaChallenge_ShouldRejectMissingSecretOrInvalidCode(bool configured)
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var identity = configured
            ? Identity(emailVerified: true, mfaConfigured: true)
            : Identity(emailVerified: true);
        SetupChallenge(store, security, "mfa-challenge");
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        security.Setup(x => x.VerifyTotp(It.IsAny<string>(), It.IsAny<string>(), Now)).Returns(false);
        var sut = new CompleteMfaChallengeCommandHandler(store.Object, security.Object, NewIssuer().Object, Time());

        var act = () => sut.Handle(
            new CompleteMfaChallengeCommand("challenge", "000000"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Refresh_ShouldRotateTokenWithoutMfaAndIssueNewAccessToken()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var issuer = NewIssuer();
        var identity = Identity(emailVerified: true, mfaConfigured: true);
        var session = AccountSession.Create(AccountId, SessionId, "old-hash", Now.AddDays(-1), Now.AddDays(1));
        store.Setup(x => x.FindSessionAsync(SessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        security.Setup(x => x.HashOpaqueToken("old-raw")).Returns("old-hash");
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>())).ReturnsAsync(identity);
        security.Setup(x => x.GenerateOpaqueToken()).Returns(new OpaqueToken("new-raw", "new-hash"));
        issuer.Setup(x => x.Issue(identity, SessionId, Now, TimeSpan.FromMinutes(15)))
            .Returns(new AccessTokenResult("new-access", Now.AddMinutes(15)));
        var sut = new RefreshSessionCommandHandler(store.Object, security.Object, issuer.Object, Time());

        var result = await sut.Handle(new RefreshSessionCommand(SessionId, "old-raw"), CancellationToken.None);

        result.SessionId.Should().Be(SessionId);
        result.RefreshToken.Should().Be("new-raw");
        result.AccessToken.Should().Be("new-access");
        session.MatchesRefreshTokenHash("new-hash").Should().BeTrue();
        store.Verify(x => x.SaveSessionAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        security.Verify(x => x.VerifyTotp(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("expired")]
    [InlineData("wrong-token")]
    public async Task Refresh_ShouldRejectInvalidSessionStates(string scenario)
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        AccountSession? session = scenario switch
        {
            "missing" => null,
            "expired" => AccountSession.Create(AccountId, SessionId, "hash", Now.AddDays(-2), Now),
            _ => AccountSession.Create(AccountId, SessionId, "expected", Now.AddDays(-1), Now.AddDays(1))
        };
        store.Setup(x => x.FindSessionAsync(SessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        security.Setup(x => x.HashOpaqueToken("raw")).Returns(scenario == "wrong-token" ? "wrong" : "hash");
        var sut = new RefreshSessionCommandHandler(store.Object, security.Object, NewIssuer().Object, Time());

        var act = () => sut.Handle(new RefreshSessionCommand(SessionId, "raw"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Refresh_ShouldRejectSessionWhoseIdentityWasRemoved()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var session = AccountSession.Create(AccountId, SessionId, "hash", Now.AddDays(-1), Now.AddDays(1));
        store.Setup(x => x.FindSessionAsync(SessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        security.Setup(x => x.HashOpaqueToken("raw")).Returns("hash");
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentity?)null);
        var sut = new RefreshSessionCommandHandler(store.Object, security.Object, NewIssuer().Object, Time());

        var act = () => sut.Handle(new RefreshSessionCommand(SessionId, "raw"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Logout_ShouldRevokeOwnedSession()
    {
        var store = new Mock<IAccountStore>();
        var session = AccountSession.Create(AccountId, SessionId, "hash", Now.AddDays(-1), Now.AddDays(1));
        store.Setup(x => x.FindSessionAsync(SessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        var sut = new LogoutSessionCommandHandler(store.Object, Time());

        await sut.Handle(new LogoutSessionCommand(AccountId, SessionId), CancellationToken.None);

        session.IsRevoked.Should().BeTrue();
        store.Verify(x => x.SaveSessionAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Logout_ShouldBeIdempotentForMissingOrForeignSession(bool foreign)
    {
        var store = new Mock<IAccountStore>();
        var session = foreign
            ? AccountSession.Create(Guid.NewGuid(), SessionId, "hash", Now.AddDays(-1), Now.AddDays(1))
            : null;
        store.Setup(x => x.FindSessionAsync(SessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        var sut = new LogoutSessionCommandHandler(store.Object, Time());

        await sut.Handle(new LogoutSessionCommand(AccountId, SessionId), CancellationToken.None);

        store.Verify(x => x.SaveSessionAsync(It.IsAny<AccountSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PasswordRecovery_ShouldStoreTokenAndSendEmailForKnownAddress()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var email = Mock.Of<IAccountEmailSender>();
        var identity = Identity();
        store.Setup(x => x.FindIdentityByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        security.Setup(x => x.GenerateOpaqueToken()).Returns(new OpaqueToken("reset-raw", "reset-hash"));
        var sut = new RequestPasswordResetCommandHandler(store.Object, security.Object, email, Time());

        await sut.Handle(new RequestPasswordResetCommand("player@example.com"), CancellationToken.None);

        store.Verify(x => x.StoreSecurityTokenAsync(
            AccountId,
            "password-reset",
            "reset-hash",
            Now,
            Now.AddMinutes(30),
            It.IsAny<CancellationToken>()), Times.Once);
        Mock.Get(email).Verify(x => x.SendPasswordResetEmailAsync(identity.Email, "reset-raw", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("unknown@example.com")]
    public async Task PasswordRecovery_ShouldNotRevealInvalidOrUnknownAddress(string email)
    {
        var store = new Mock<IAccountStore>();
        store.Setup(x => x.FindIdentityByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentity?)null);
        var security = NewSecurity();
        var sender = new Mock<IAccountEmailSender>();
        var sut = new RequestPasswordResetCommandHandler(store.Object, security.Object, sender.Object, Time());

        await sut.Handle(new RequestPasswordResetCommand(email), CancellationToken.None);

        security.Verify(x => x.GenerateOpaqueToken(), Times.Never);
        sender.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ShouldChangeHashConsumeTokenAndRevokeAllSessions()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        var identity = Identity();
        security.Setup(x => x.HashOpaqueToken("reset-raw")).Returns("reset-hash");
        security.Setup(x => x.HashPassword("newcorrecthorse")).Returns("new-password-hash");
        store.Setup(x => x.FindSecurityTokenAsync("password-reset", "reset-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Token("password-reset", Now.AddMinutes(1)));
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        var sut = new ResetPasswordCommandHandler(store.Object, security.Object, Time());

        await sut.Handle(new ResetPasswordCommand("reset-raw", "newcorrecthorse"), CancellationToken.None);

        identity.PasswordHash.Should().Be("new-password-hash");
        store.Verify(x => x.SaveIdentityAsync(identity, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.ConsumeSecurityTokenAsync(TokenId, Now, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.RevokeSessionsAsync(AccountId, Now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ShouldRejectExpiredTokenBeforeChangingPassword()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        security.Setup(x => x.HashOpaqueToken("reset-raw")).Returns("reset-hash");
        store.Setup(x => x.FindSecurityTokenAsync("password-reset", "reset-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Token("password-reset", Now));
        var sut = new ResetPasswordCommandHandler(store.Object, security.Object, Time());

        var act = () => sut.Handle(new ResetPasswordCommand("reset-raw", "newcorrecthorse"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task ResetPassword_ShouldFailIfAccountIdentityWasRemoved()
    {
        var store = new Mock<IAccountStore>();
        var security = NewSecurity();
        security.Setup(x => x.HashOpaqueToken("reset-raw")).Returns("reset-hash");
        store.Setup(x => x.FindSecurityTokenAsync("password-reset", "reset-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Token("password-reset", Now.AddMinutes(1)));
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentity?)null);
        var sut = new ResetPasswordCommandHandler(store.Object, security.Object, Time());

        var act = () => sut.Handle(new ResetPasswordCommand("reset-raw", "newcorrecthorse"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void SecurityTokenState_ShouldOnlyBeConsumableBeforeExpirationAndBeforeConsumption()
    {
        Token("test", Now.AddSeconds(1)).CanConsume(Now).Should().BeTrue();
        Token("test", Now).CanConsume(Now).Should().BeFalse();
        new SecurityTokenState(TokenId, AccountId, "test", "hash", Now.AddMinutes(-1), Now.AddMinutes(1), Now)
            .CanConsume(Now).Should().BeFalse();
    }

    private static Mock<IAuthenticationSecurity> NewSecurity() => new(MockBehavior.Loose);
    private static Mock<IAccessTokenIssuer> NewIssuer() => new(MockBehavior.Loose);
    private static FrozenTimeProvider Time() => new(Now);

    private static UserIdentity Identity(bool emailVerified = false, bool mfaConfigured = false)
    {
        var identity = UserIdentity.RegisterForAccount(
            AccountId,
            EmailAddress.Create("player@example.com"),
            "argon2id$unit-test-hash",
            Now.AddDays(-2));
        if (emailVerified)
            identity.VerifyEmail(Now.AddDays(-1));
        if (mfaConfigured)
            identity.ConfigureMfa("protected-totp", Now.AddHours(-1));
        return identity;
    }

    private static SecurityTokenState Token(string purpose, DateTimeOffset expiresAt, DateTimeOffset? consumedAt = null) =>
        new(TokenId, AccountId, purpose, "challenge-hash", Now.AddMinutes(-1), expiresAt, consumedAt);

    private static void SetupChallenge(
        Mock<IAccountStore> store,
        Mock<IAuthenticationSecurity> security,
        string purpose)
    {
        security.Setup(x => x.HashOpaqueToken("challenge")).Returns("challenge-hash");
        store.Setup(x => x.FindSecurityTokenAsync(purpose, "challenge-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Token(purpose, Now.AddMinutes(1)));
    }

    private sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
