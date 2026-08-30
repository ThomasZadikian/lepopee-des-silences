using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerPermanentUnlockTests
{
    [Fact]
    public void Create_ShouldCreateValidUnlock()
    {
        var now = DateTimeOffset.UtcNow;
        var unlock = PlayerPermanentUnlock.Create("unlock.key", "Skill", null, now);

        unlock.UnlockKey.Should().Be("unlock.key");
        unlock.UnlockType.Should().Be("Skill");
        unlock.SourceRunId.Should().BeNull();
        unlock.UnlockedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Create_ShouldCreateUnlockWithSourceRunId()
    {
        var now = DateTimeOffset.UtcNow;
        var runId = Guid.NewGuid();
        var unlock = PlayerPermanentUnlock.Create("unlock.key", "Skill", runId, now);

        unlock.SourceRunId.Should().Be(runId);
    }

    [Fact]
    public void Create_ShouldTrimUnlockKey()
    {
        var now = DateTimeOffset.UtcNow;
        var unlock = PlayerPermanentUnlock.Create("  unlock.key  ", "Skill", null, now);

        unlock.UnlockKey.Should().Be("unlock.key");
    }

    [Fact]
    public void Create_ShouldTrimUnlockType()
    {
        var now = DateTimeOffset.UtcNow;
        var unlock = PlayerPermanentUnlock.Create("unlock.key", "  Skill  ", null, now);

        unlock.UnlockType.Should().Be("Skill");
    }

    [Fact]
    public void Create_ShouldRejectNullUnlockKey()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => PlayerPermanentUnlock.Create(null!, "Skill", null, now);

        act.Should().Throw<DomainException>().WithMessage("*Unlock key*");
    }

    [Fact]
    public void Create_ShouldRejectEmptyUnlockKey()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => PlayerPermanentUnlock.Create("", "Skill", null, now);

        act.Should().Throw<DomainException>().WithMessage("*Unlock key*");
    }

    [Fact]
    public void Create_ShouldRejectWhitespaceUnlockKey()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => PlayerPermanentUnlock.Create("   ", "Skill", null, now);

        act.Should().Throw<DomainException>().WithMessage("*Unlock key*");
    }

    [Fact]
    public void Create_ShouldRejectNullUnlockType()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => PlayerPermanentUnlock.Create("unlock.key", null!, null, now);

        act.Should().Throw<DomainException>().WithMessage("*Unlock type*");
    }

    [Fact]
    public void Create_ShouldRejectEmptyUnlockType()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => PlayerPermanentUnlock.Create("unlock.key", "", null, now);

        act.Should().Throw<DomainException>().WithMessage("*Unlock type*");
    }

    [Fact]
    public void Create_ShouldRejectWhitespaceUnlockType()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => PlayerPermanentUnlock.Create("unlock.key", "   ", null, now);

        act.Should().Throw<DomainException>().WithMessage("*Unlock type*");
    }

    [Fact]
    public void Properties_ShouldBeImmutable()
    {
        var now = DateTimeOffset.UtcNow;
        var runId = Guid.NewGuid();
        var unlock = PlayerPermanentUnlock.Create("unlock.key", "Skill", runId, now);

        unlock.UnlockKey.Should().Be("unlock.key");
        unlock.UnlockType.Should().Be("Skill");
        unlock.SourceRunId.Should().Be(runId);
        unlock.UnlockedAtUtc.Should().Be(now);
    }

    [Theory]
    [InlineData("Character")]
    [InlineData("Skill")]
    [InlineData("Item")]
    [InlineData("Achievement")]
    public void Create_ShouldAcceptVariousUnlockTypes(string unlockType)
    {
        var now = DateTimeOffset.UtcNow;

        var unlock = PlayerPermanentUnlock.Create("unlock.key", unlockType, null, now);

        unlock.UnlockType.Should().Be(unlockType);
    }
}
