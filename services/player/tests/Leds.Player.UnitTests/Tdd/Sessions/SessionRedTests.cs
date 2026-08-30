using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.UnitTests.Tdd;

namespace Leds.Player.UnitTests.Tdd.Sessions;

public sealed class SessionRedTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan GameLeaseDuration = TimeSpan.FromMinutes(2);

    [Fact]
    public void AccountSession_ShouldAcceptMultipleIndependentSessionsForTheSameAccount()
    {
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sessionType = FutureContract.RequireDomainType("Leds.Player.Domain.Sessions.AccountSession");

        var desktop = FutureContract.InvokeStatic(
            sessionType,
            "Create",
            accountId,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "refresh-hash-desktop",
            Now,
            Now.AddDays(30));
        var mobile = FutureContract.InvokeStatic(
            sessionType,
            "Create",
            accountId,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "refresh-hash-mobile",
            Now,
            Now.AddDays(30));

        FutureContract.Read<Guid>(desktop, "AccountId").Should().Be(accountId);
        FutureContract.Read<Guid>(mobile, "AccountId").Should().Be(accountId);
        FutureContract.Read<Guid>(desktop, "SessionId")
            .Should().NotBe(FutureContract.Read<Guid>(mobile, "SessionId"));
    }

    [Fact]
    public void RefreshRotation_ShouldInvalidateThePreviousRefreshTokenHash()
    {
        var session = CreateAccountSession("old-refresh-hash");

        FutureContract.InvokeInstance(
            session,
            "RotateRefreshToken",
            "new-refresh-hash",
            Now.AddDays(30),
            Now.AddMinutes(5));

        FutureContract.InvokeInstance(session, "MatchesRefreshTokenHash", "old-refresh-hash")
            .Should().Be(false);
        FutureContract.InvokeInstance(session, "MatchesRefreshTokenHash", "new-refresh-hash")
            .Should().Be(true);
    }

    [Fact]
    public void RevokedSession_ShouldRejectItsRefreshToken()
    {
        var session = CreateAccountSession("refresh-hash");

        FutureContract.InvokeInstance(session, "Revoke", Now.AddMinutes(5));

        FutureContract.Read<bool>(session, "IsRevoked").Should().BeTrue();
        FutureContract.InvokeInstance(session, "MatchesRefreshTokenHash", "refresh-hash")
            .Should().Be(false);
    }

    [Fact]
    public void ExpiredSession_ShouldBeReportedAsExpired()
    {
        var session = CreateAccountSession("refresh-hash", expiresAtUtc: Now.AddMinutes(10));

        FutureContract.InvokeInstance(session, "IsExpired", Now.AddMinutes(9)).Should().Be(false);
        FutureContract.InvokeInstance(session, "IsExpired", Now.AddMinutes(10)).Should().Be(true);
    }

    [Fact]
    public void ActiveGameSession_ShouldUseTheApprovedTwoMinuteLease()
    {
        var ownerSessionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var lease = CreateGameLease(ownerSessionId);

        FutureContract.Read<DateTimeOffset>(lease, "ExpiresAtUtc")
            .Should().Be(Now.Add(GameLeaseDuration));
        FutureContract.InvokeInstance(lease, "IsExpired", Now.Add(GameLeaseDuration).AddTicks(-1))
            .Should().Be(false);
        FutureContract.InvokeInstance(lease, "IsExpired", Now.Add(GameLeaseDuration))
            .Should().Be(true);
    }

    [Fact]
    public void Heartbeat_ShouldExtendOnlyTheCurrentGameSessionLease()
    {
        var ownerSessionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var lease = CreateGameLease(ownerSessionId);

        FutureContract.InvokeInstance(
            lease,
            "Heartbeat",
            ownerSessionId,
            Now.AddMinutes(1),
            GameLeaseDuration);

        FutureContract.Read<DateTimeOffset>(lease, "ExpiresAtUtc")
            .Should().Be(Now.AddMinutes(3));
    }

    [Fact]
    public void Heartbeat_FromAnotherDevice_ShouldBeRejected()
    {
        var lease = CreateGameLease(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var otherSessionId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var act = () => FutureContract.InvokeInstance(
            lease,
            "Heartbeat",
            otherSessionId,
            Now.AddMinutes(1),
            GameLeaseDuration);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ConfirmedTransfer_ShouldMoveTheExclusiveGameLeaseToTheNewDevice()
    {
        var oldSessionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var newSessionId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var lease = CreateGameLease(oldSessionId);

        FutureContract.InvokeInstance(
            lease,
            "Transfer",
            newSessionId,
            Now.AddSeconds(30),
            GameLeaseDuration);

        FutureContract.Read<Guid>(lease, "OwnerSessionId").Should().Be(newSessionId);
        FutureContract.Read<DateTimeOffset>(lease, "ExpiresAtUtc")
            .Should().Be(Now.AddSeconds(30).Add(GameLeaseDuration));
    }

    private static object CreateAccountSession(
        string refreshTokenHash,
        DateTimeOffset? expiresAtUtc = null)
    {
        var sessionType = FutureContract.RequireDomainType("Leds.Player.Domain.Sessions.AccountSession");
        return FutureContract.InvokeStatic(
            sessionType,
            "Create",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            refreshTokenHash,
            Now,
            expiresAtUtc ?? Now.AddDays(30));
    }

    private static object CreateGameLease(Guid ownerSessionId)
    {
        var leaseType = FutureContract.RequireDomainType("Leds.Player.Domain.Sessions.ActiveGameSessionLease");
        return FutureContract.InvokeStatic(
            leaseType,
            "Acquire",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ownerSessionId,
            Now,
            GameLeaseDuration);
    }
}
