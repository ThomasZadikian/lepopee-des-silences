using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Accounts;
using Leds.Player.Domain.Identity;
using Leds.Player.Domain.Players;
using Leds.Player.Domain.Privacy;
using Leds.Player.Domain.Sessions;
using Moq;

namespace Leds.Player.UnitTests.Application.Accounts;

public sealed class AccountPrivacyAndSettingsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpdateProfile_ShouldRenameTheAccountWithoutTouchingProgression()
    {
        var profile = PlayerProfile.Create("Avant", Now.AddDays(-1));
        profile.AdvanceMainStory("main", "1", "step-2", null, ["hall"], ["hall"], false, Now.AddHours(-2));
        var renamedProfile = PlayerProfile.Rehydrate(
            profile.Id,
            "Après",
            profile.Roster,
            profile.Progression,
            profile.CreatedAtUtc,
            Now,
            profile.MainStoryProgress,
            profile.PermanentUnlocks,
            profile.PermanentItems,
            profile.NpcReputationScores);
        var identity = UserIdentity.RegisterForAccount(
            profile.Id.Value,
            EmailAddress.Create("player@example.com"),
            "hash",
            Now.AddDays(-1));
        var maintenance = new Mock<IAccountProfileMaintenance>();
        var store = new Mock<IAccountStore>();
        var profiles = new Mock<IPlayerProfileRepository>();
        store.Setup(x => x.FindIdentityByAccountIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        profiles.Setup(x => x.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(renamedProfile);

        var result = await new UpdateAccountProfileCommandHandler(
            maintenance.Object,
            store.Object,
            profiles.Object,
            Time())
            .Handle(new UpdateAccountProfileCommand(profile.Id.Value, "Après"), CancellationToken.None);

        result.DisplayName.Should().Be("Après");
        renamedProfile.MainStoryProgress.StepKey.Should().Be("step-2");
        maintenance.Verify(x => x.RenameAsync(
            profile.Id,
            "Après",
            Now,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeEmail_ShouldRequireVerificationOfTheNewAddress()
    {
        var store = new Mock<IAccountStore>();
        var security = new Mock<IAuthenticationSecurity>();
        var sender = new Mock<IAccountEmailSender>();
        var identity = Identity();
        security.Setup(x => x.GenerateOpaqueToken()).Returns(new OpaqueToken("raw-token", "token-hash"));
        store.Setup(x => x.FindIdentityByAccountIdAsync(identity.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);

        var result = await new ChangeAccountEmailCommandHandler(store.Object, security.Object, sender.Object, Time())
            .Handle(new ChangeAccountEmailCommand(identity.AccountId, "new@example.com"), CancellationToken.None);

        result.Email.Should().Be("new@example.com");
        result.VerificationRequired.Should().BeTrue();
        identity.IsEmailVerified.Should().BeFalse();
        store.Verify(x => x.StoreSecurityTokenAsync(
            identity.AccountId,
            "email-verification",
            "token-hash",
            Now,
            Now.AddHours(24),
            It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(x => x.SendVerificationEmailAsync(
            EmailAddress.Create("new@example.com"),
            "raw-token",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OptionalConsent_ShouldBeHistorisedWhenGrantedThenRevoked()
    {
        var store = new Mock<IAccountStore>();
        PrivacyConsent? persisted = null;
        store.Setup(x => x.ListConsentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => persisted is null ? [] : [persisted]);
        store.Setup(x => x.SaveConsentAsync(It.IsAny<Guid>(), It.IsAny<PrivacyConsent>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, PrivacyConsent, CancellationToken>((_, consent, _) => persisted = consent)
            .Returns(Task.CompletedTask);
        var accountId = Guid.NewGuid();
        var handler = new SetPrivacyConsentCommandHandler(store.Object, Time());

        var granted = await handler.Handle(
            new SetPrivacyConsentCommand(accountId, "analytics.gameplay", "1.0", true),
            CancellationToken.None);
        var revoked = await handler.Handle(
            new SetPrivacyConsentCommand(accountId, "analytics.gameplay", "1.0", false),
            CancellationToken.None);

        granted.IsGranted.Should().BeTrue();
        revoked.IsGranted.Should().BeFalse();
        revoked.RevokedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Closure_ShouldHaveThirtyDayGracePeriodAndRemainCancellable()
    {
        var store = new Mock<IAccountStore>();
        AccountClosureRequest? closure = null;
        store.Setup(x => x.GetClosureRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => closure);
        store.Setup(x => x.SaveClosureRequestAsync(It.IsAny<AccountClosureRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AccountClosureRequest, CancellationToken>((request, _) => closure = request)
            .Returns(Task.CompletedTask);
        var accountId = Guid.NewGuid();

        var requested = await new RequestAccountClosureCommandHandler(store.Object, Time())
            .Handle(new RequestAccountClosureCommand(accountId), CancellationToken.None);
        var cancelled = await new CancelAccountClosureCommandHandler(store.Object, Time())
            .Handle(new CancelAccountClosureCommand(accountId), CancellationToken.None);

        requested.ExecuteAfterUtc.Should().Be(Now.AddDays(30));
        cancelled.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task Export_ShouldReturnReadableStructuredAccountData()
    {
        var profile = PlayerProfile.Create("Nocturne", Now.AddDays(-30));
        profile.CreatePlayableCharacter("Aube", "Porteur", Now.AddDays(-20));
        var identity = UserIdentity.RegisterForAccount(
            profile.Id.Value,
            EmailAddress.Create("player@example.com"),
            "hash",
            Now.AddDays(-30));
        identity.VerifyEmail(Now.AddDays(-29));
        var store = new Mock<IAccountStore>();
        var profiles = new Mock<IPlayerProfileRepository>();
        store.Setup(x => x.FindIdentityByAccountIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
        store.Setup(x => x.ListConsentsAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync([PrivacyConsent.Grant("analytics.gameplay", "1.0", Now.AddDays(-10))]);
        store.Setup(x => x.ListSessionsAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AccountSession.Create(profile.Id.Value, Guid.NewGuid(), "hash", Now.AddDays(-1), Now.AddDays(29))]);
        profiles.Setup(x => x.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var export = await new GetAccountDataExportQueryHandler(store.Object, profiles.Object, Time())
            .Handle(new GetAccountDataExportQuery(profile.Id.Value), CancellationToken.None);

        export.Format.Should().Be("LEDS-account-export-v1");
        export.Identity.DisplayName.Should().Be("Nocturne");
        export.Identity.Email.Should().Be("player@example.com");
        export.Characters.Should().ContainSingle(c => c.DisplayName == "Aube");
        export.Consents.Should().ContainSingle(c => c.PurposeKey == "analytics.gameplay");
    }

    [Fact]
    public async Task DueClosure_ShouldAnonymiseProfileAndPurgeAuthenticationMaterial()
    {
        var accountId = Guid.NewGuid();
        var maintenance = new Mock<IAccountPrivacyMaintenanceStore>();
        var profiles = new Mock<IAccountProfileMaintenance>();
        maintenance.Setup(x => x.ListExecutableClosureAccountIdsAsync(Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync([accountId]);

        var processed = await new ExecuteDueAccountClosuresCommandHandler(
            maintenance.Object,
            profiles.Object,
            Time())
            .Handle(new ExecuteDueAccountClosuresCommand(), CancellationToken.None);

        processed.Should().Be(1);
        profiles.Verify(x => x.AnonymizeAsync(
            new PlayerId(accountId),
            Now,
            It.IsAny<CancellationToken>()), Times.Once);
        maintenance.Verify(x => x.PurgeAuthenticationMaterialAsync(
            accountId,
            Now,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UserIdentity Identity()
    {
        var identity = UserIdentity.Register(
            EmailAddress.Create("old@example.com"),
            "hash",
            Now.AddDays(-2));
        identity.VerifyEmail(Now.AddDays(-1));
        return identity;
    }

    private static TimeProvider Time() => new FixedTimeProvider(Now);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
