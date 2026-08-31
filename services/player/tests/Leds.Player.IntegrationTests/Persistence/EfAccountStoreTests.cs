using FluentAssertions;
using Leds.Player.Domain.Identity;
using Leds.Player.Domain.Players;
using Leds.Player.Domain.Privacy;
using Leds.Player.Domain.Sessions;
using Leds.Player.Infrastructure.Persistence.Repositories;

namespace Leds.Player.IntegrationTests.Persistence;

[Collection("PlayerPostgres")]
public sealed class EfAccountStoreTests
{
    private readonly PlayerPostgresFixture _fixture;
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 8, 0, 0, TimeSpan.Zero);

    public EfAccountStoreTests(PlayerPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Store_ShouldPersistCompleteIdentitySessionPrivacyAndGameLeaseLifecycle()
    {
        var (context, connectionString) = _fixture.CreateContext();
        await using var _ = context;
        var store = new EfAccountStore(context);

        var profile = PlayerProfile.Create("Nocturne", Now);
        var identity = UserIdentity.RegisterForAccount(
            profile.Id.Value,
            EmailAddress.Create("player@example.com"),
            "password-hash",
            Now);

        (await store.EmailExistsAsync(identity.Email, CancellationToken.None)).Should().BeFalse();
        await store.RegisterAsync(
            profile,
            identity,
            "verification-hash",
            Now.AddHours(24),
            CancellationToken.None);

        (await store.EmailExistsAsync(identity.Email, CancellationToken.None)).Should().BeTrue();
        (await store.FindIdentityByEmailAsync(identity.Email, CancellationToken.None))!.AccountId
            .Should().Be(profile.Id.Value);
        (await store.FindIdentityByAccountIdAsync(profile.Id.Value, CancellationToken.None))!.Email
            .Should().Be(identity.Email);
        (await store.FindIdentityByEmailAsync(EmailAddress.Create("missing@example.com"), CancellationToken.None))
            .Should().BeNull();
        (await store.FindIdentityByAccountIdAsync(Guid.NewGuid(), CancellationToken.None))
            .Should().BeNull();

        var verification = await store.FindSecurityTokenAsync(
            "email-verification",
            "verification-hash",
            CancellationToken.None);
        verification.Should().NotBeNull();
        verification!.AccountId.Should().Be(profile.Id.Value);
        await store.ConsumeSecurityTokenAsync(verification.Id, Now.AddMinutes(1), CancellationToken.None);
        await store.ConsumeSecurityTokenAsync(verification.Id, Now.AddMinutes(2), CancellationToken.None);
        (await store.FindSecurityTokenAsync("email-verification", "verification-hash", CancellationToken.None))!
            .ConsumedAtUtc.Should().Be(Now.AddMinutes(1));
        (await store.FindSecurityTokenAsync("missing", "missing", CancellationToken.None)).Should().BeNull();

        identity.VerifyEmail(Now.AddMinutes(2));
        identity.ConfigureMfa("protected-mfa", Now.AddMinutes(3));
        identity.ChangePasswordHash("new-password-hash");
        await store.SaveIdentityAsync(identity, CancellationToken.None);
        var reloadedIdentity = await store.FindIdentityByAccountIdAsync(profile.Id.Value, CancellationToken.None);
        reloadedIdentity!.IsEmailVerified.Should().BeTrue();
        reloadedIdentity.IsMfaConfigured.Should().BeTrue();
        reloadedIdentity.PasswordHash.Should().Be("new-password-hash");

        await store.StoreSecurityTokenAsync(
            profile.Id.Value,
            "password-reset",
            "reset-hash",
            Now.AddMinutes(4),
            Now.AddMinutes(34),
            CancellationToken.None);
        (await store.FindSecurityTokenAsync("password-reset", "reset-hash", CancellationToken.None))
            .Should().NotBeNull();

        var desktop = AccountSession.Create(
            profile.Id.Value,
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            "desktop-refresh",
            Now,
            Now.AddDays(30));
        var mobile = AccountSession.Create(
            profile.Id.Value,
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            "mobile-refresh",
            Now.AddMinutes(1),
            Now.AddDays(30));
        await store.AddSessionAsync(desktop, CancellationToken.None);
        await store.AddSessionAsync(mobile, CancellationToken.None);

        (await store.FindSessionAsync(desktop.SessionId, CancellationToken.None))!.SessionId
            .Should().Be(desktop.SessionId);
        (await store.FindSessionAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeNull();
        var sessions = await store.ListSessionsAsync(profile.Id.Value, CancellationToken.None);
        sessions.Should().HaveCount(2);
        sessions.First().SessionId.Should().Be(mobile.SessionId);

        desktop.RotateRefreshToken("desktop-refresh-2", Now.AddDays(31), Now.AddMinutes(5));
        await store.SaveSessionAsync(desktop, CancellationToken.None);
        var reloadedDesktop = await store.FindSessionAsync(desktop.SessionId, CancellationToken.None);
        reloadedDesktop!.RefreshTokenHash.Should().Be("desktop-refresh-2");
        reloadedDesktop.RotatedAtUtc.Should().Be(Now.AddMinutes(5));

        await store.RevokeSessionsAsync(profile.Id.Value, Now.AddMinutes(6), CancellationToken.None);
        (await store.ListSessionsAsync(profile.Id.Value, CancellationToken.None))
            .Should().OnlyContain(session => session.IsRevoked);

        (await store.ListConsentsAsync(profile.Id.Value, CancellationToken.None)).Should().BeEmpty();
        var consent = PrivacyConsent.Grant("optional.analytics", "1.0", Now.AddMinutes(7));
        await store.SaveConsentAsync(profile.Id.Value, consent, CancellationToken.None);
        var consents = await store.ListConsentsAsync(profile.Id.Value, CancellationToken.None);
        consents.Should().ContainSingle(c => c.IsGranted && c.PurposeKey == "optional.analytics");

        consent.Revoke(Now.AddMinutes(8));
        await store.SaveConsentAsync(profile.Id.Value, consent, CancellationToken.None);
        (await store.ListConsentsAsync(profile.Id.Value, CancellationToken.None))
            .Should().ContainSingle(c => !c.IsGranted && c.RevokedAtUtc == Now.AddMinutes(8));

        (await store.GetClosureRequestAsync(profile.Id.Value, CancellationToken.None)).Should().BeNull();
        var closure = AccountClosureRequest.Request(profile.Id.Value, Now.AddMinutes(9), TimeSpan.FromDays(30));
        await store.SaveClosureRequestAsync(closure, CancellationToken.None);
        var reloadedClosure = await store.GetClosureRequestAsync(profile.Id.Value, CancellationToken.None);
        reloadedClosure.Should().NotBeNull();
        reloadedClosure!.ExecuteAfterUtc.Should().Be(Now.AddMinutes(9).AddDays(30));

        closure.Cancel(Now.AddDays(1));
        await store.SaveClosureRequestAsync(closure, CancellationToken.None);
        (await store.GetClosureRequestAsync(profile.Id.Value, CancellationToken.None))!.IsCancelled
            .Should().BeTrue();

        (await store.GetGameLeaseAsync(profile.Id.Value, CancellationToken.None)).Should().BeNull();
        var lease = ActiveGameSessionLease.Acquire(
            profile.Id.Value,
            desktop.SessionId,
            Now.AddMinutes(10),
            TimeSpan.FromMinutes(2));
        await store.SaveGameLeaseAsync(lease, CancellationToken.None);
        var reloadedLease = await store.GetGameLeaseAsync(profile.Id.Value, CancellationToken.None);
        reloadedLease!.OwnerSessionId.Should().Be(desktop.SessionId);

        lease.Transfer(mobile.SessionId, Now.AddMinutes(11), TimeSpan.FromMinutes(2));
        await store.SaveGameLeaseAsync(lease, CancellationToken.None);
        reloadedLease = await store.GetGameLeaseAsync(profile.Id.Value, CancellationToken.None);
        reloadedLease!.OwnerSessionId.Should().Be(mobile.SessionId);
        reloadedLease.ExpiresAtUtc.Should().Be(Now.AddMinutes(13));

        await using var verifyContext = _fixture.CreateContext(connectionString);
        var verifyStore = new EfAccountStore(verifyContext);
        (await verifyStore.FindIdentityByAccountIdAsync(profile.Id.Value, CancellationToken.None))
            .Should().NotBeNull("all account data must survive a completely fresh EF change tracker");
    }
}
