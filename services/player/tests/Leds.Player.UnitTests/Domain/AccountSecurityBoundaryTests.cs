using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Identity;
using Leds.Player.Domain.Privacy;
using Leds.Player.Domain.Sessions;

namespace Leds.Player.UnitTests.Domain;

public sealed class AccountSecurityBoundaryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void AccountSession_Create_ShouldRejectInvalidRequiredState(
        bool emptyAccount,
        bool emptySession,
        bool blankHash)
    {
        var act = () => AccountSession.Create(
            emptyAccount ? Guid.Empty : AccountId,
            emptySession ? Guid.Empty : SessionId,
            blankHash ? " " : "refresh-hash",
            Now,
            Now.AddDays(1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AccountSession_Create_ShouldRejectNonFutureExpiration()
    {
        var act = () => AccountSession.Create(AccountId, SessionId, "hash", Now, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AccountSession_Rehydrate_ShouldPreserveRotationAndRevocation()
    {
        var session = AccountSession.Rehydrate(
            AccountId,
            SessionId,
            "hash",
            Now,
            Now.AddDays(1),
            Now.AddMinutes(10),
            Now.AddMinutes(20));

        session.RotatedAtUtc.Should().Be(Now.AddMinutes(10));
        session.RevokedAtUtc.Should().Be(Now.AddMinutes(20));
        session.IsRevoked.Should().BeTrue();
        session.MatchesRefreshTokenHash("hash").Should().BeFalse();
    }

    [Fact]
    public void AccountSession_Rotate_ShouldRejectRevokedSession()
    {
        var session = ValidSession();
        session.Revoke(Now.AddMinutes(1));

        var act = () => session.RotateRefreshToken("new-hash", Now.AddDays(2), Now.AddMinutes(2));

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AccountSession_Rotate_ShouldRejectInvalidHashOrExpiration(bool blankHash, bool badExpiration)
    {
        var session = ValidSession();
        var rotatedAt = Now.AddMinutes(1);

        var act = () => session.RotateRefreshToken(
            blankHash ? " " : "new-hash",
            badExpiration ? rotatedAt : Now.AddDays(2),
            rotatedAt);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AccountSession_Matching_ShouldRejectEmptyOrDifferentLengthHash()
    {
        var session = ValidSession();

        session.MatchesRefreshTokenHash(string.Empty).Should().BeFalse();
        session.MatchesRefreshTokenHash("x").Should().BeFalse();
    }

    [Fact]
    public void AccountSession_Revoke_ShouldBeIdempotent()
    {
        var session = ValidSession();
        session.Revoke(Now.AddMinutes(1));
        session.Revoke(Now.AddMinutes(2));

        session.RevokedAtUtc.Should().Be(Now.AddMinutes(1));
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void GameLease_Acquire_ShouldRejectInvalidInputs(
        bool emptyAccount,
        bool emptyOwner,
        bool invalidDuration)
    {
        var act = () => ActiveGameSessionLease.Acquire(
            emptyAccount ? Guid.Empty : AccountId,
            emptyOwner ? Guid.Empty : SessionId,
            Now,
            invalidDuration ? TimeSpan.Zero : TimeSpan.FromMinutes(2));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GameLease_Rehydrate_ShouldRejectMissingIds()
    {
        var emptyAccount = () => ActiveGameSessionLease.Rehydrate(
            Guid.Empty, SessionId, Now, Now.AddMinutes(2));
        var emptyOwner = () => ActiveGameSessionLease.Rehydrate(
            AccountId, Guid.Empty, Now, Now.AddMinutes(2));

        emptyAccount.Should().Throw<DomainException>();
        emptyOwner.Should().Throw<DomainException>();
    }

    [Fact]
    public void GameLease_Rehydrate_ShouldRejectInvalidExpiration()
    {
        var act = () => ActiveGameSessionLease.Rehydrate(AccountId, SessionId, Now, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GameLease_Heartbeat_ShouldRejectNonPositiveDurationAndExpiredLease()
    {
        var lease = ValidLease();
        var badDuration = () => lease.Heartbeat(SessionId, Now.AddSeconds(30), TimeSpan.Zero);
        var expired = () => lease.Heartbeat(SessionId, Now.AddMinutes(2), TimeSpan.FromMinutes(2));

        badDuration.Should().Throw<DomainException>();
        expired.Should().Throw<DomainException>();
    }

    [Fact]
    public void GameLease_Transfer_ShouldRejectMissingOwnerAndNonPositiveDuration()
    {
        var lease = ValidLease();
        var missingOwner = () => lease.Transfer(Guid.Empty, Now.AddSeconds(10), TimeSpan.FromMinutes(2));
        var badDuration = () => lease.Transfer(Guid.NewGuid(), Now.AddSeconds(10), TimeSpan.Zero);

        missingOwner.Should().Throw<DomainException>();
        badDuration.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AccountClosure_Request_ShouldRejectInvalidInputs(bool emptyAccount, bool invalidGracePeriod)
    {
        var act = () => AccountClosureRequest.Request(
            emptyAccount ? Guid.Empty : AccountId,
            Now,
            invalidGracePeriod ? TimeSpan.Zero : TimeSpan.FromDays(30));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AccountClosure_Rehydrate_ShouldRejectInvalidState()
    {
        var emptyAccount = () => AccountClosureRequest.Rehydrate(
            Guid.Empty, Now, Now.AddDays(30), null);
        var badExecution = () => AccountClosureRequest.Rehydrate(
            AccountId, Now, Now, null);

        emptyAccount.Should().Throw<DomainException>();
        badExecution.Should().Throw<DomainException>();
    }

    [Fact]
    public void AccountClosure_Cancel_ShouldRejectLateCancellationAndRemainIdempotent()
    {
        var late = AccountClosureRequest.Request(AccountId, Now, TimeSpan.FromDays(30));
        var lateAct = () => late.Cancel(Now.AddDays(30));
        lateAct.Should().Throw<DomainException>();

        var request = AccountClosureRequest.Request(AccountId, Now, TimeSpan.FromDays(30));
        request.Cancel(Now.AddDays(1));
        request.Cancel(Now.AddDays(2));
        request.CancelledAtUtc.Should().Be(Now.AddDays(1));
        request.CanExecute(Now.AddDays(31)).Should().BeFalse();
    }

    [Theory]
    [InlineData("", "1.0")]
    [InlineData("optional.analytics", "")]
    public void PrivacyConsent_ShouldRejectBlankPurposeOrPolicy(string purpose, string policy)
    {
        var act = () => PrivacyConsent.Grant(purpose, policy, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void PrivacyConsent_ShouldRejectNecessaryProcessingAsConsent()
    {
        var act = () => PrivacyConsent.Grant("necessary.gameplay", "1.0", Now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PrivacyConsent_Rehydrate_ShouldRejectRevocationBeforeGrant()
    {
        var act = () => PrivacyConsent.Rehydrate(
            "optional.analytics", "1.0", Now, Now.AddSeconds(-1));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void PrivacyConsent_Revoke_ShouldRejectEarlierTimestampAndBeIdempotent()
    {
        var consent = PrivacyConsent.Grant("optional.analytics", "1.0", Now);
        var early = () => consent.Revoke(Now.AddSeconds(-1));
        early.Should().Throw<DomainException>();

        consent.Revoke(Now.AddDays(1));
        consent.Revoke(Now.AddDays(2));
        consent.RevokedAtUtc.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void UserIdentity_ShouldRejectEmptyAccountAndBlankPasswordHash()
    {
        var email = EmailAddress.Create("player@example.com");
        var emptyAccount = () => UserIdentity.RegisterForAccount(Guid.Empty, email, "hash", Now);
        var blankHash = () => UserIdentity.RegisterForAccount(AccountId, email, " ", Now);

        emptyAccount.Should().Throw<DomainException>();
        blankHash.Should().Throw<DomainException>();
    }

    [Fact]
    public void UserIdentity_VerifyEmail_ShouldBeIdempotent()
    {
        var identity = ValidIdentity();
        identity.VerifyEmail(Now.AddMinutes(1));
        identity.VerifyEmail(Now.AddMinutes(2));

        identity.EmailVerifiedAtUtc.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void UserIdentity_ConfigureMfa_ShouldRejectBlankSecretAfterVerification()
    {
        var identity = ValidIdentity();
        identity.VerifyEmail(Now);

        var act = () => identity.ConfigureMfa(" ", Now.AddMinutes(1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UserIdentity_ChangePassword_ShouldRejectBlankHash()
    {
        var act = () => ValidIdentity().ChangePasswordHash(" ");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UserIdentity_Rehydrate_ShouldRejectMissingIdentityOrAccountId()
    {
        var email = EmailAddress.Create("player@example.com");
        var missingIdentity = () => UserIdentity.Rehydrate(new UserIdentitySnapshot
        {
            Id = Guid.Empty,
            AccountId = AccountId,
            Email = email,
            PasswordHash = "hash",
            Role = AccountRole.Player,
            CreatedAtUtc = Now
        });
        var missingAccount = () => UserIdentity.Rehydrate(new UserIdentitySnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.Empty,
            Email = email,
            PasswordHash = "hash",
            Role = AccountRole.Player,
            CreatedAtUtc = Now
        });

        missingIdentity.Should().Throw<DomainException>();
        missingAccount.Should().Throw<DomainException>();
    }

    private static AccountSession ValidSession() =>
        AccountSession.Create(AccountId, SessionId, "refresh-hash", Now, Now.AddDays(1));

    private static ActiveGameSessionLease ValidLease() =>
        ActiveGameSessionLease.Acquire(AccountId, SessionId, Now, TimeSpan.FromMinutes(2));

    private static UserIdentity ValidIdentity() =>
        UserIdentity.RegisterForAccount(
            AccountId,
            EmailAddress.Create("player@example.com"),
            "password-hash",
            Now);
}
