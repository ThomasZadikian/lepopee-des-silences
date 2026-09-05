using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Accounts;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Identity;
using Leds.Player.Domain.Players;
using Leds.Player.Domain.Sessions;
using Moq;

namespace Leds.Player.UnitTests.Application.Accounts;

public sealed class AccountManagementAndRecoveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherSessionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ChallengeId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void RecoveryCodeService_ShouldGenerateUniqueCodesAndStableHashes()
    {
        var sut = new DefaultRecoveryCodeService();

        var batch = sut.Generate(10);

        batch.RawCodes.Should().HaveCount(10).And.OnlyHaveUniqueItems();
        batch.Hashes.Should().HaveCount(10).And.OnlyHaveUniqueItems();
        batch.RawCodes.Should().OnlyContain(code => code.Length == 23 && code.Count(c => c == '-') == 3);
        sut.Hash(batch.RawCodes.First().ToLowerInvariant()).Should().Be(batch.Hashes.First());
        sut.Hash(batch.RawCodes.First().Replace("-", string.Empty)).Should().Be(batch.Hashes.First());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void RecoveryCodeService_ShouldRejectInvalidRequestedCount(int count)
    {
        var act = () => new DefaultRecoveryCodeService().Generate(count);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void RecoveryCodeService_ShouldRejectInvalidPresentedCode(string value)
    {
        var act = () => new DefaultRecoveryCodeService().Hash(value);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UserIdentity_ShouldStageMfaSecretWithoutEnablingMfa()
    {
        var identity = Identity(emailVerified: true);

        identity.StageMfaSecret(" staged-secret ");

        identity.MfaSecretProtected.Should().Be("staged-secret");
        identity.IsMfaConfigured.Should().BeFalse();
    }

    [Fact]
    public void UserIdentity_ShouldConsumePersistedRecoveryCodeOnlyOnce()
    {
        var identity = Identity(emailVerified: true);
        identity.ConfigureMfa("protected", Now, ["hash-a", "hash-b"]);

        identity.TryConsumeRecoveryCodeHash("hash-b").Should().BeTrue();
        identity.TryConsumeRecoveryCodeHash("hash-b").Should().BeFalse();
        identity.RecoveryCodeHashes.Should().BeEquivalentTo(["hash-a"]);
    }

    [Fact]
    public async Task MfaRecovery_ShouldConsumeCodeAndCreateInteractiveSession()
    {
        var store = new Mock<IAccountStore>();
        var security = new Mock<IAuthenticationSecurity>();
        var issuer = new Mock<IAccessTokenIssuer>();
        var recovery = new Mock<IRecoveryCodeService>();
        var identity = Identity(emailVerified: true);
        identity.ConfigureMfa("protected", Now.AddHours(-1), ["recovery-hash"]);

        security.Setup(x => x.HashOpaqueToken("challenge")).Returns("challenge-hash");
        store.Setup(x => x.FindSecurityTokenAsync("mfa-challenge", "challenge-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityTokenState(
                ChallengeId,
                AccountId,
                "mfa-challenge",
                "challenge-hash",
                Now.AddMinutes(-1),
                Now.AddMinutes(4),
                null));
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        recovery.Setup(x => x.Hash("RECOVERY-CODE")).Returns("recovery-hash");
        security.Setup(x => x.GenerateOpaqueToken()).Returns(new OpaqueToken("refresh", "refresh-hash"));
        issuer.Setup(x => x.Issue(identity, It.IsAny<Guid>(), Now, TimeSpan.FromMinutes(15)))
            .Returns(new AccessTokenResult("access", Now.AddMinutes(15)));

        var sut = new CompleteMfaRecoveryCodeCommandHandler(
            store.Object,
            security.Object,
            issuer.Object,
            recovery.Object,
            Time());

        var result = await sut.Handle(
            new CompleteMfaRecoveryCodeCommand("challenge", "RECOVERY-CODE"),
            CancellationToken.None);

        result.AccessToken.Should().Be("access");
        identity.RecoveryCodeHashes.Should().BeEmpty();
        store.Verify(x => x.SaveIdentityAsync(identity, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.ConsumeSecurityTokenAsync(ChallengeId, Now, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.AddSessionAsync(It.IsAny<AccountSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MfaRecovery_ShouldRejectMalformedOrUnknownCode(bool malformed)
    {
        var store = new Mock<IAccountStore>();
        var security = new Mock<IAuthenticationSecurity>();
        var issuer = new Mock<IAccessTokenIssuer>();
        var recovery = new Mock<IRecoveryCodeService>();
        var identity = Identity(emailVerified: true);
        identity.ConfigureMfa("protected", Now.AddHours(-1), ["valid-hash"]);

        security.Setup(x => x.HashOpaqueToken("challenge")).Returns("challenge-hash");
        store.Setup(x => x.FindSecurityTokenAsync("mfa-challenge", "challenge-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityTokenState(
                ChallengeId, AccountId, "mfa-challenge", "challenge-hash", Now.AddMinutes(-1), Now.AddMinutes(1), null));
        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        if (malformed)
            recovery.Setup(x => x.Hash(It.IsAny<string>())).Throws(new DomainException("invalid"));
        else
            recovery.Setup(x => x.Hash(It.IsAny<string>())).Returns("unknown-hash");

        var sut = new CompleteMfaRecoveryCodeCommandHandler(
            store.Object, security.Object, issuer.Object, recovery.Object, Time());

        var act = () => sut.Handle(
            new CompleteMfaRecoveryCodeCommand("challenge", "bad"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        identity.RecoveryCodeHashes.Should().ContainSingle().Which.Should().Be("valid-hash");
        store.Verify(x => x.SaveIdentityAsync(It.IsAny<UserIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Overview_ShouldReturnOnlyActivePlayerCreatedCharacters()
    {
        var store = new Mock<IAccountStore>();
        var profiles = new Mock<IPlayerProfileRepository>();
        var identity = Identity(emailVerified: true);
        var profile = PlayerProfile.Create("Nocturne", Now.AddDays(-1));
        profile.CreatePlayableCharacter("Aube", "archetype.porteur", Now.AddHours(-2));
        var archived = profile.CreatePlayableCharacter("Cendre", "archetype.porteur", Now.AddHours(-1));
        archived.Archive(Now.AddMinutes(-30));
        profile.RecruitCompanion(
            "character.companion.test",
            "Compagnon",
            PlayerCharacterStatBlock.CreateDefaultPorteur(),
            ["skill.basic.guard"],
            Now.AddMinutes(-10));

        store.Setup(x => x.FindIdentityByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        profiles.Setup(x => x.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await new GetAccountOverviewQueryHandler(store.Object, profiles.Object)
            .Handle(new GetAccountOverviewQuery(AccountId), CancellationToken.None);

        result.DisplayName.Should().Be("Nocturne");
        result.EmailVerified.Should().BeTrue();
        result.Characters.Should().ContainSingle(c => c.DisplayName == "Aube");
    }

    [Fact]
    public async Task Sessions_ShouldMarkCurrentAndOnlyNonExpiredNonRevokedSessionsActive()
    {
        var store = new Mock<IAccountStore>();
        var current = AccountSession.Create(AccountId, SessionId, "hash-a", Now.AddDays(-1), Now.AddDays(1));
        var expired = AccountSession.Create(AccountId, OtherSessionId, "hash-b", Now.AddDays(-2), Now);
        var revoked = AccountSession.Create(AccountId, Guid.NewGuid(), "hash-c", Now.AddDays(-1), Now.AddDays(1));
        revoked.Revoke(Now.AddMinutes(-1));
        store.Setup(x => x.ListSessionsAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([current, expired, revoked]);

        var result = await new ListAccountSessionsQueryHandler(store.Object, Time())
            .Handle(new ListAccountSessionsQuery(AccountId, SessionId), CancellationToken.None);

        result.Single(x => x.SessionId == SessionId).IsCurrent.Should().BeTrue();
        result.Single(x => x.SessionId == SessionId).IsActive.Should().BeTrue();
        result.Single(x => x.SessionId == OtherSessionId).IsActive.Should().BeFalse();
        result.Single(x => x.RevokedAtUtc.HasValue).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeSession_ShouldRevokeOwnedSessionAndReleaseItsGameLease()
    {
        var store = new Mock<IAccountStore>();
        var session = AccountSession.Create(AccountId, OtherSessionId, "hash", Now.AddDays(-1), Now.AddDays(1));
        store.Setup(x => x.FindSessionAsync(OtherSessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        await new RevokeAccountSessionCommandHandler(store.Object, Time())
            .Handle(new RevokeAccountSessionCommand(AccountId, OtherSessionId), CancellationToken.None);

        session.IsRevoked.Should().BeTrue();
        store.Verify(x => x.SaveSessionAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.ReleaseGameLeaseAsync(AccountId, OtherSessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAndArchiveCharacter_ShouldPersistAccountRoster()
    {
        var profiles = new Mock<IPlayerProfileRepository>();
        var profile = PlayerProfile.Create("Nocturne", Now.AddDays(-1));
        profiles.Setup(x => x.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var archetypes = new Mock<IArchetypeDefinitionGateway>();
        archetypes.Setup(item => item.GetByKeyAsync("archetype.porteur", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchetypeDefinitionSnapshot(
                "archetype.porteur", PlayerCharacterStatBlock.CreateDefaultPorteur(), [], [],
                ["skill.basic.guard"], ["skill.basic.guard"]));
        var created = await new CreateAccountCharacterCommandHandler(profiles.Object, Time(), archetypes.Object)
            .Handle(
                new CreateAccountCharacterCommand(profile.Id.Value, "Aube", "archetype.porteur"),
                CancellationToken.None);
        var character = created.Characters.Single(c => c.DisplayName == "Aube");
        character.ArchetypeKey.Should().Be("archetype.porteur");

        var archived = await new ArchiveAccountCharacterCommandHandler(profiles.Object, Time())
            .Handle(
                new ArchiveAccountCharacterCommand(profile.Id.Value, character.Id),
                CancellationToken.None);

        archived.Characters.Single(c => c.Id == character.Id).IsArchived.Should().BeTrue();
        profiles.Verify(x => x.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GameSessionClaim_ShouldRequireConfirmationBeforeTransfer()
    {
        var store = ActiveSessionStore();
        var existing = ActiveGameSessionLease.Acquire(AccountId, OtherSessionId, Now.AddMinutes(-1), TimeSpan.FromMinutes(2));
        store.Setup(x => x.ClaimGameLeaseAsync(
                AccountId, SessionId, Now, TimeSpan.FromMinutes(2), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameLeaseClaimResult(existing, false, true));

        var result = await new ClaimGameSessionCommandHandler(store.Object, Time())
            .Handle(new ClaimGameSessionCommand(AccountId, SessionId, false), CancellationToken.None);

        result.Status.Should().Be("transfer-required");
        result.OwnerSessionId.Should().Be(OtherSessionId);
    }

    [Fact]
    public async Task GameSessionClaim_ShouldTransferOnlyAfterConfirmation()
    {
        var store = ActiveSessionStore();
        var transferred = ActiveGameSessionLease.Acquire(AccountId, SessionId, Now, TimeSpan.FromMinutes(2));
        store.Setup(x => x.ClaimGameLeaseAsync(
                AccountId, SessionId, Now, TimeSpan.FromMinutes(2), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameLeaseClaimResult(transferred, true, false));

        var result = await new ClaimGameSessionCommandHandler(store.Object, Time())
            .Handle(new ClaimGameSessionCommand(AccountId, SessionId, true), CancellationToken.None);

        result.Status.Should().Be("active");
        result.ExpiresAtUtc.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public async Task GameSessionClaim_ShouldRejectRevokedSession()
    {
        var store = new Mock<IAccountStore>();
        var session = AccountSession.Create(AccountId, SessionId, "hash", Now.AddDays(-1), Now.AddDays(1));
        session.Revoke(Now.AddMinutes(-1));
        store.Setup(x => x.FindSessionAsync(SessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var act = () => new ClaimGameSessionCommandHandler(store.Object, Time())
            .Handle(new ClaimGameSessionCommand(AccountId, SessionId, false), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        store.Verify(x => x.ClaimGameLeaseAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GameSessionHeartbeat_ShouldRenewOwnedLease()
    {
        var store = ActiveSessionStore();
        var lease = ActiveGameSessionLease.Acquire(AccountId, SessionId, Now.AddMinutes(-1), TimeSpan.FromMinutes(2));
        lease.Heartbeat(SessionId, Now, TimeSpan.FromMinutes(2));
        store.Setup(x => x.HeartbeatGameLeaseAsync(
                AccountId, SessionId, Now, TimeSpan.FromMinutes(2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        store.Setup(x => x.GetGameLeaseAsync(AccountId, It.IsAny<CancellationToken>())).ReturnsAsync(lease);

        var result = await new HeartbeatGameSessionCommandHandler(store.Object, Time())
            .Handle(new HeartbeatGameSessionCommand(AccountId, SessionId), CancellationToken.None);

        result.Status.Should().Be("active");
        result.OwnerSessionId.Should().Be(SessionId);
    }

    [Fact]
    public async Task GameSessionHeartbeat_ShouldFailAfterLeaseOwnershipIsLost()
    {
        var store = ActiveSessionStore();
        store.Setup(x => x.HeartbeatGameLeaseAsync(
                AccountId, SessionId, Now, TimeSpan.FromMinutes(2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => new HeartbeatGameSessionCommandHandler(store.Object, Time())
            .Handle(new HeartbeatGameSessionCommand(AccountId, SessionId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ReleaseGameSession_ShouldDelegateToAtomicStoreOperation()
    {
        var store = new Mock<IAccountStore>();

        await new ReleaseGameSessionCommandHandler(store.Object)
            .Handle(new ReleaseGameSessionCommand(AccountId, SessionId), CancellationToken.None);

        store.Verify(x => x.ReleaseGameLeaseAsync(AccountId, SessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IAccountStore> ActiveSessionStore()
    {
        var store = new Mock<IAccountStore>();
        store.Setup(x => x.FindSessionAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountSession.Create(
                AccountId,
                SessionId,
                "refresh-hash",
                Now.AddDays(-1),
                Now.AddDays(1)));
        return store;
    }

    private static UserIdentity Identity(bool emailVerified = false)
    {
        var identity = UserIdentity.RegisterForAccount(
            AccountId,
            EmailAddress.Create("player@example.com"),
            "argon-hash",
            Now.AddDays(-2));
        if (emailVerified)
            identity.VerifyEmail(Now.AddDays(-1));
        return identity;
    }

    private static FrozenTimeProvider Time() => new(Now);

    private sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
