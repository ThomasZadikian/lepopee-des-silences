using FluentAssertions;
using Leds.Player.Domain.Players;
using Leds.Player.UnitTests.Tdd;

namespace Leds.Player.UnitTests.Tdd.Privacy;

public sealed class GdprRedTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan ClosureGracePeriod = TimeSpan.FromDays(30);

    [Fact]
    public void AccountClosure_ShouldWaitThirtyDaysBeforeAnonymizationCanExecute()
    {
        var request = CreateClosureRequest();

        FutureContract.Read<DateTimeOffset>(request, "ExecuteAfterUtc")
            .Should().Be(Now.Add(ClosureGracePeriod));
        FutureContract.InvokeInstance(request, "CanExecute", Now.AddDays(29).AddHours(23))
            .Should().Be(false);
        FutureContract.InvokeInstance(request, "CanExecute", Now.Add(ClosureGracePeriod))
            .Should().Be(true);
    }

    [Fact]
    public void AccountClosure_ShouldBeCancelableDuringTheGracePeriod()
    {
        var request = CreateClosureRequest();

        FutureContract.InvokeInstance(request, "Cancel", Now.AddDays(10));

        FutureContract.Read<bool>(request, "IsCancelled").Should().BeTrue();
        FutureContract.InvokeInstance(request, "CanExecute", Now.AddDays(31))
            .Should().Be(false);
    }

    [Fact]
    public void AccountAnonymization_ShouldRemoveThePlayerDisplayNameButPreserveGameProgression()
    {
        var account = PlayerProfile.Create("PersonallyIdentifyingAlias", Now);
        account.AwardCurrency(Now.AddMinutes(1), 17);
        account.GrantPermanentUnlock(
            "unlock.room.hall",
            "room",
            sourceRunId: null,
            Now.AddMinutes(2));

        FutureContract.InvokeInstance(
            account,
            "Anonymize",
            "anonymous-11111111",
            Now.AddDays(31));

        account.DisplayName.Should().Be("anonymous-11111111");
        account.Progression.PalaceShardCount.Should().Be(17);
        account.HasPermanentUnlock("unlock.room.hall").Should().BeTrue(
            "anonymization removes personal identifiers, not the non-identifying game history required for referential integrity");
    }

    [Fact]
    public void Consent_ShouldBeIndependentlyRevocableAndHistorized()
    {
        var consentType = FutureContract.RequireDomainType("Leds.Player.Domain.Privacy.PrivacyConsent");
        var consent = FutureContract.InvokeStatic(
            consentType,
            "Grant",
            "optional.analytics",
            "2026-08",
            Now);

        FutureContract.Read<bool>(consent, "IsGranted").Should().BeTrue();

        FutureContract.InvokeInstance(consent, "Revoke", Now.AddDays(5));

        FutureContract.Read<bool>(consent, "IsGranted").Should().BeFalse();
        FutureContract.Read<DateTimeOffset?>(consent, "RevokedAtUtc")
            .Should().Be(Now.AddDays(5));
        FutureContract.Read<string>(consent, "PurposeKey")
            .Should().Be("optional.analytics");
        FutureContract.Read<string>(consent, "PolicyVersion")
            .Should().Be("2026-08");
    }

    [Fact]
    public void NecessaryGameplayProcessing_ShouldNotBeRepresentedAsOptionalConsent()
    {
        var consentType = FutureContract.RequireDomainType("Leds.Player.Domain.Privacy.PrivacyConsent");

        var act = () => FutureContract.InvokeStatic(
            consentType,
            "Grant",
            "necessary.gameplay",
            "2026-08",
            Now);

        act.Should().Throw<InvalidOperationException>(
            "mandatory gameplay processing must use its legal basis rather than a revocable consent switch");
    }

    private static object CreateClosureRequest()
    {
        var requestType = FutureContract.RequireDomainType("Leds.Player.Domain.Privacy.AccountClosureRequest");
        return FutureContract.InvokeStatic(
            requestType,
            "Request",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Now,
            ClosureGracePeriod);
    }
}
