using FluentAssertions;
using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.UnitTests.Domain.RewardCursePools;

public sealed class RewardCurseEntryTests
{
    [Fact]
    public void Create_ShouldCreateEntry_WithRewardKind()
    {
        var entry = new RewardCurseEntry(
            RewardCurseEntryKind.Reward,
            "Heal",
            Amount: 10);

        entry.Kind.Should().Be(RewardCurseEntryKind.Reward);
        entry.ResultKind.Should().Be("Heal");
        entry.Amount.Should().Be(10);
        entry.TargetKey.Should().BeNull();
        entry.Availability.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCreateEntry_WithCurseKind()
    {
        var entry = new RewardCurseEntry(
            RewardCurseEntryKind.Curse,
            "GrantCurse",
            TargetKey: "curse-shadow-blight");

        entry.Kind.Should().Be(RewardCurseEntryKind.Curse);
        entry.ResultKind.Should().Be("GrantCurse");
        entry.TargetKey.Should().Be("curse-shadow-blight");
        entry.Amount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldCreateEntry_WithDefaultValues_WhenOptionalParametersOmitted()
    {
        var entry = new RewardCurseEntry(
            RewardCurseEntryKind.Reward,
            "Damage");

        entry.TargetKey.Should().BeNull();
        entry.Amount.Should().Be(0);
        entry.Availability.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCreateEntry_WithAvailabilityConstraints()
    {
        var availability = new[]
        {
            new RewardCurseAvailability(RewardCursePoolFilterKind.MinNodeDepth, 3),
            new RewardCurseAvailability(RewardCursePoolFilterKind.MinActiveLawCount, 2)
        };

        var entry = new RewardCurseEntry(
            RewardCurseEntryKind.Reward,
            "Gold",
            Amount: 50,
            Availability: availability);

        entry.Availability.Should().HaveCount(2);
        entry.Availability.Should().Contain(a => a.Kind == RewardCursePoolFilterKind.MinNodeDepth && a.Value == 3);
        entry.Availability.Should().Contain(a => a.Kind == RewardCursePoolFilterKind.MinActiveLawCount && a.Value == 2);
    }

    [Fact]
    public void Create_ShouldCreateEntry_WithAllResultKinds()
    {
        var resultKinds = new[] { "Heal", "Damage", "Guard", "Mana", "GrantCurse", "GrantLaw" };

        foreach (var resultKind in resultKinds)
        {
            var entry = new RewardCurseEntry(
                RewardCurseEntryKind.Reward,
                resultKind);

            entry.ResultKind.Should().Be(resultKind);
        }
    }

    [Fact]
    public void RecordEquality_ShouldBeEqual_WhenAllPropertiesMatch()
    {
        var entry1 = new RewardCurseEntry(
            RewardCurseEntryKind.Reward,
            "Heal",
            Amount: 10);

        var entry2 = new RewardCurseEntry(
            RewardCurseEntryKind.Reward,
            "Heal",
            Amount: 10);

        entry1.Should().Be(entry2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenKindDiffers()
    {
        var entry1 = new RewardCurseEntry(
            RewardCurseEntryKind.Reward,
            "Heal");

        var entry2 = new RewardCurseEntry(
            RewardCurseEntryKind.Curse,
            "Heal");

        entry1.Should().NotBe(entry2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenAmountDiffers()
    {
        var entry1 = new RewardCurseEntry(
            RewardCurseEntryKind.Reward,
            "Heal",
            Amount: 10);

        var entry2 = new RewardCurseEntry(
            RewardCurseEntryKind.Reward,
            "Heal",
            Amount: 20);

        entry1.Should().NotBe(entry2);
    }
}
