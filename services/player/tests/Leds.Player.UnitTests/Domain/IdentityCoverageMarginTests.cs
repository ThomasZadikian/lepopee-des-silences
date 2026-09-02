using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Identity;

namespace Leds.Player.UnitTests.Domain;

public sealed class IdentityCoverageMarginTests
{
    private static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RecoveryCodes_ShouldValidateInputAndConsumeEachHashOnlyOnce()
    {
        var nullAct = () => RecoveryCodeSet.Create(null!);
        nullAct.Should().Throw<ArgumentNullException>();

        var emptyAct = () => RecoveryCodeSet.Create([]);
        emptyAct.Should().Throw<DomainException>();

        var whitespaceAct = () => RecoveryCodeSet.Create(["hash-a", " "]);
        whitespaceAct.Should().Throw<DomainException>();

        var codes = RecoveryCodeSet.Create(["hash-a", "hash-b"]);
        codes.RemainingCount.Should().Be(2);
        codes.TryConsume("").Should().BeFalse();
        codes.TryConsume("missing").Should().BeFalse();
        codes.TryConsume("hash-a").Should().BeTrue();
        codes.TryConsume("hash-a").Should().BeFalse();
        codes.RemainingCount.Should().Be(1);
    }

    [Fact]
    public void EmailVerificationToken_ShouldCoverValidationExpiryMismatchAndSingleUse()
    {
        var missingHash = () => EmailVerificationToken.Issue(AccountId, " ", Now, TimeSpan.FromMinutes(5));
        missingHash.Should().Throw<DomainException>();

        var invalidLifetime = () => EmailVerificationToken.Issue(AccountId, "hash", Now, TimeSpan.Zero);
        invalidLifetime.Should().Throw<DomainException>();

        var token = EmailVerificationToken.Issue(AccountId, "hash", Now, TimeSpan.FromMinutes(5));
        token.TryConsume("longer-hash", Now.AddMinutes(1)).Should().BeFalse();
        token.TryConsume("nope", Now.AddMinutes(1)).Should().BeFalse();
        token.TryConsume("hash", token.ExpiresAtUtc).Should().BeFalse();
        token.TryConsume("hash", Now.AddMinutes(2)).Should().BeTrue();
        token.TryConsume("hash", Now.AddMinutes(3)).Should().BeFalse();
        token.ConsumedAtUtc.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public void PasswordResetToken_ShouldCoverValidationExpiryMismatchAndSingleUse()
    {
        var missingHash = () => PasswordResetToken.Issue(AccountId, "", Now, TimeSpan.FromMinutes(10));
        missingHash.Should().Throw<DomainException>();

        var invalidLifetime = () => PasswordResetToken.Issue(AccountId, "hash", Now, TimeSpan.FromSeconds(-1));
        invalidLifetime.Should().Throw<DomainException>();

        var token = PasswordResetToken.Issue(AccountId, "hash", Now, TimeSpan.FromMinutes(10));
        token.TryConsume("longer-hash", Now.AddMinutes(1)).Should().BeFalse();
        token.TryConsume("nope", Now.AddMinutes(1)).Should().BeFalse();
        token.TryConsume("hash", token.ExpiresAtUtc.AddSeconds(1)).Should().BeFalse();
        token.TryConsume("hash", Now.AddMinutes(2)).Should().BeTrue();
        token.TryConsume("hash", Now.AddMinutes(3)).Should().BeFalse();
        token.ConsumedAtUtc.Should().Be(Now.AddMinutes(2));
    }
}
