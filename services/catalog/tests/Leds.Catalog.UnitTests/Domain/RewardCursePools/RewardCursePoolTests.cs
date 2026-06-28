using System.Collections.ObjectModel;
using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.UnitTests.Domain.RewardCursePools;

public sealed class RewardCursePoolTests
{
    [Fact]
    public void Create_ShouldCreatePool_WhenAllParametersAreValid()
    {
        var pool = RewardCursePool.Create(
            "pool-reward-curse-basic",
            "Basic Reward/Curse Pool",
            "A standard pool with mixed rewards and curses.",
            "catalog-0.1.0");

        pool.Key.Value.Should().Be("pool-reward-curse-basic");
        pool.Name.Value.Should().Be("Basic Reward/Curse Pool");
        pool.Description.Value.Should().Be("A standard pool with mixed rewards and curses.");
        pool.Version.Value.Should().Be("catalog-0.1.0");
        pool.Status.Should().Be(CatalogContentStatus.Draft);
        pool.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldCreatePoolWithEntries_WhenEntriesAreProvided()
    {
        var entries = new[]
        {
            new RewardCurseEntry(
                RewardCurseEntryKind.Reward,
                "Heal",
                Amount: 10),
            new RewardCurseEntry(
                RewardCurseEntryKind.Curse,
                "GrantCurse",
                TargetKey: "curse-shadow-blight")
        };

        var pool = RewardCursePool.Create(
            "pool-with-entries",
            "Pool With Entries",
            "Description",
            "catalog-0.1.0",
            entries);

        pool.Entries.Should().HaveCount(2);
        pool.Entries.Should().Contain(e => e.Kind == RewardCurseEntryKind.Reward);
        pool.Entries.Should().Contain(e => e.Kind == RewardCurseEntryKind.Curse);
    }

    [Fact]
    public void Create_ShouldCreatePoolWithEmptyEntries_WhenEntriesIsNull()
    {
        var pool = RewardCursePool.Create(
            "pool-null-entries",
            "Pool",
            "Description",
            "catalog-0.1.0",
            entries: null);

        pool.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldSetStatusToActive_WhenStatusIsProvidedAsActive()
    {
        var pool = RewardCursePool.Create(
            "pool-active",
            "Active Pool",
            "Description",
            "catalog-0.1.0",
            status: CatalogContentStatus.Active);

        pool.Status.Should().Be(CatalogContentStatus.Active);
        pool.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldAssignNewId_WhenCreated()
    {
        var pool1 = RewardCursePool.Create("pool-1", "Pool 1", "Desc", "v1");
        var pool2 = RewardCursePool.Create("pool-2", "Pool 2", "Desc", "v1");

        pool1.Id.Should().NotBe(pool2.Id);
        pool1.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenKeyIsEmpty()
    {
        var act = () => RewardCursePool.Create(
            string.Empty, "Name", "Desc", "v1");

        act.Should().Throw<DomainException>()
            .WithMessage("Catalog content key is required.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenNameIsEmpty()
    {
        var act = () => RewardCursePool.Create(
            "valid-key", string.Empty, "Desc", "v1");

        act.Should().Throw<DomainException>()
            .WithMessage("Catalog content name is required.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenVersionIsEmpty()
    {
        var act = () => RewardCursePool.Create(
            "valid-key", "Name", "Desc", string.Empty);

        act.Should().Throw<DomainException>()
            .WithMessage("Catalog version is required.");
    }

    [Fact]
    public void Entries_ShouldReturnReadOnlyCollection_WhenAccessed()
    {
        var entries = new[]
        {
            new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", Amount: 5)
        };

        var pool = RewardCursePool.Create(
            "pool-readonly",
            "Pool",
            "Desc",
            "v1",
            entries);

        pool.Entries.Should().BeOfType<ReadOnlyCollection<RewardCurseEntry>>();
    }

    [Fact]
    public void Activate_ShouldChangeStatusToActive_WhenStatusIsDraft()
    {
        var pool = RewardCursePool.Create("pool-activate", "Pool", "Desc", "v1");

        pool.Activate();

        pool.Status.Should().Be(CatalogContentStatus.Active);
        pool.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deprecate_ShouldChangeStatusToDeprecated_WhenStatusIsActive()
    {
        var pool = RewardCursePool.Create(
            "pool-deprecate", "Pool", "Desc", "v1",
            status: CatalogContentStatus.Active);

        pool.Deprecate();

        pool.Status.Should().Be(CatalogContentStatus.Deprecated);
        pool.IsDeprecated.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldChangeStatusToDisabled_WhenStatusIsDraft()
    {
        var pool = RewardCursePool.Create("pool-disable", "Pool", "Desc", "v1");

        pool.Disable();

        pool.Status.Should().Be(CatalogContentStatus.Disabled);
        pool.IsDisabled.Should().BeTrue();
    }
}
