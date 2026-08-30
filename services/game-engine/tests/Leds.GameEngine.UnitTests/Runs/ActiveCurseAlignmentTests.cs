using FluentAssertions;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class ActiveCurseAlignmentTests
{
    [Fact]
    public void Create_ShouldSetAllNewFields()
    {
        var curse = ActiveCurse.Create(
            "curse-test",
            "Test Curse",
            "A test curse",
            0.10,
            DateTime.UtcNow,
            curseDefinitionKey: "curse.test.v1",
            severity: 5,
            duration: "NextCombatOnly",
            effectSetKey: "effectset.curse-test");

        curse.Id.Should().NotBeEmpty();
        curse.Key.Should().Be("curse-test");
        curse.CurseDefinitionKey.Should().Be("curse.test.v1");
        curse.Severity.Should().Be(5);
        curse.Duration.Should().Be("NextCombatOnly");
        curse.EffectSetKey.Should().Be("effectset.curse-test");
        curse.ConsumedAtUtc.Should().BeNull();
        curse.IsConsumed.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldUseDefaultDuration_WhenNotSpecified()
    {
        var curse = ActiveCurse.Create(
            "curse-test",
            "Test Curse",
            "A test curse",
            0.10,
            DateTime.UtcNow);

        curse.Duration.Should().Be("NextCombatOnly");
    }

    [Fact]
    public void Consume_ShouldSetConsumedAtUtc()
    {
        var curse = ActiveCurse.Create(
            "curse-test",
            "Test Curse",
            "A test curse",
            0.10,
            DateTime.UtcNow);

        var consumedAt = DateTime.UtcNow;
        curse.Consume(consumedAt);

        curse.ConsumedAtUtc.Should().Be(consumedAt);
        curse.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void Consume_ShouldBeIdempotent()
    {
        var curse = ActiveCurse.Create(
            "curse-test",
            "Test Curse",
            "A test curse",
            0.10,
            DateTime.UtcNow);

        var first = DateTime.UtcNow;
        var second = first.AddMinutes(1);
        curse.Consume(first);
        curse.Consume(second);

        curse.ConsumedAtUtc.Should().Be(first);
    }

    [Fact]
    public void Rehydrate_ShouldAcceptAllNewFields()
    {
        var appliedAt = DateTime.UtcNow;
        var consumedAt = DateTime.UtcNow.AddMinutes(5);

        var curse = ActiveCurse.Rehydrate(
            Guid.NewGuid(),
            "curse-test",
            "Test Curse",
            "A test curse",
            0.10,
            appliedAt,
            curseDefinitionKey: "curse.test.v1",
            severity: 7,
            duration: "UntilRunEnds",
            expiresAtRoomId: Guid.NewGuid(),
            consumedAtUtc: consumedAt,
            effectSetKey: "effectset.curse-test");

        curse.CurseDefinitionKey.Should().Be("curse.test.v1");
        curse.Severity.Should().Be(7);
        curse.Duration.Should().Be("UntilRunEnds");
        curse.ExpiresAtRoomId.Should().NotBeNull();
        curse.ConsumedAtUtc.Should().Be(consumedAt);
        curse.IsConsumed.Should().BeTrue();
        curse.EffectSetKey.Should().Be("effectset.curse-test");
    }
}
