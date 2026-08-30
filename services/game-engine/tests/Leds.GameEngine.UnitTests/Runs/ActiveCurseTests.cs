using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class ActiveCurseTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var appliedAt = DateTime.UtcNow;
        var curse = ActiveCurse.Create(
            "curse-fragile",
            "Fragilité",
            "Réduit la défense.",
            0.2,
            appliedAt);

        curse.Id.Should().NotBeEmpty();
        curse.Key.Should().Be("curse-fragile");
        curse.DisplayName.Should().Be("Fragilité");
        curse.Description.Should().Be("Réduit la défense.");
        curse.DifficultyDelta.Should().Be(0.2);
        curse.AppliedAtUtc.Should().Be(appliedAt);
        curse.Severity.Should().Be(3);
        curse.Duration.Should().Be("NextCombatOnly");
        curse.ExpiresAtRoomId.Should().BeNull();
        curse.ConsumedAtUtc.Should().BeNull();
        curse.IsConsumed.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldTrimKeyDisplayNameAndDescription()
    {
        var curse = ActiveCurse.Create(
            "  curse  ",
            "  Name  ",
            "  Desc  ",
            0.1,
            DateTime.UtcNow);

        curse.Key.Should().Be("curse");
        curse.DisplayName.Should().Be("Name");
        curse.Description.Should().Be("Desc");
    }

    [Fact]
    public void Create_ShouldHandleNullDescription()
    {
        var curse = ActiveCurse.Create(
            "curse",
            "Name",
            null,
            0.1,
            DateTime.UtcNow);

        curse.Description.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldThrow_WhenKeyIsNullOrWhitespace()
    {
        var act1 = () => ActiveCurse.Create("", "Name", "Desc", 0.1, DateTime.UtcNow);
        var act2 = () => ActiveCurse.Create("   ", "Name", "Desc", 0.1, DateTime.UtcNow);

        act1.Should().Throw<DomainException>().WithMessage("Curse key is required.");
        act2.Should().Throw<DomainException>().WithMessage("Curse key is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDisplayNameIsNullOrWhitespace()
    {
        var act1 = () => ActiveCurse.Create("key", "", "Desc", 0.1, DateTime.UtcNow);
        var act2 = () => ActiveCurse.Create("key", "   ", "Desc", 0.1, DateTime.UtcNow);

        act1.Should().Throw<DomainException>().WithMessage("Curse display name is required.");
        act2.Should().Throw<DomainException>().WithMessage("Curse display name is required.");
    }

    [Theory]
    [InlineData(-0.6)]
    [InlineData(0.6)]
    [InlineData(-1.0)]
    [InlineData(1.0)]
    public void Create_ShouldThrow_WhenDifficultyDeltaIsOutOfBounds(double delta)
    {
        var act = () => ActiveCurse.Create("key", "Name", "Desc", delta, DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Curse difficulty delta must be between -0.5 and 0.5.");
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(0.0)]
    [InlineData(0.5)]
    public void Create_ShouldAcceptBoundaryDifficultyDelta(double delta)
    {
        var act = () => ActiveCurse.Create("key", "Name", "Desc", delta, DateTime.UtcNow);

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_ShouldAcceptCustomSeverityAndDuration()
    {
        var curse = ActiveCurse.Create(
            "curse",
            "Name",
            "Desc",
            0.1,
            DateTime.UtcNow,
            severity: 5,
            duration: "UntilRunEnds");

        curse.Severity.Should().Be(5);
        curse.Duration.Should().Be("UntilRunEnds");
    }

    [Fact]
    public void Consume_ShouldSetConsumedAtUtc()
    {
        var curse = ActiveCurse.Create("key", "Name", "Desc", 0.1, DateTime.UtcNow);
        var consumedAt = DateTime.UtcNow;

        curse.Consume(consumedAt);

        curse.ConsumedAtUtc.Should().Be(consumedAt);
        curse.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void Consume_ShouldNotChangeConsumedAtUtc_WhenAlreadyConsumed()
    {
        var curse = ActiveCurse.Create("key", "Name", "Desc", 0.1, DateTime.UtcNow);
        var firstConsumedAt = DateTime.UtcNow;
        var secondConsumedAt = DateTime.UtcNow.AddMinutes(5);

        curse.Consume(firstConsumedAt);
        curse.Consume(secondConsumedAt);

        curse.ConsumedAtUtc.Should().Be(firstConsumedAt);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreCurse_WithGivenValues()
    {
        var id = Guid.NewGuid();
        var appliedAt = DateTime.UtcNow.AddDays(-1);
        var consumedAt = DateTime.UtcNow;

        var curse = ActiveCurse.Rehydrate(
            id,
            "curse",
            "Name",
            "Desc",
            0.3,
            appliedAt,
            severity: 4,
            duration: "UntilRoomEnds",
            consumedAtUtc: consumedAt);

        curse.Id.Should().Be(id);
        curse.Key.Should().Be("curse");
        curse.DifficultyDelta.Should().Be(0.3);
        curse.Severity.Should().Be(4);
        curse.Duration.Should().Be("UntilRoomEnds");
        curse.ConsumedAtUtc.Should().Be(consumedAt);
    }
}
