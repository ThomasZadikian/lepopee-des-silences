using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerPermanentItemTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldRejectMissingDefinitionKey(string key)
    {
        var act = () => PlayerPermanentItem.Create(key, null, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>()
            .WithMessage("Item definition key is required.");
    }

    [Fact]
    public void SetContainedLiquid_ShouldRejectMissingLiquidKey()
    {
        var item = PlayerPermanentItem.Create("item.flask", null, DateTimeOffset.UtcNow);

        var act = () => item.SetContainedLiquid("   ");

        act.Should().Throw<DomainException>()
            .WithMessage("Liquid definition key is required.");
    }

    [Fact]
    public void SetContainedLiquid_ShouldRejectReplacingExistingLiquid()
    {
        var item = PlayerPermanentItem.Create("item.flask", null, DateTimeOffset.UtcNow);
        item.SetContainedLiquid("liquid.water");

        var act = () => item.SetContainedLiquid("liquid.tea");

        act.Should().Throw<DomainException>()
            .WithMessage("*already holds a liquid*");
        item.ContainedLiquidDefinitionKey.Should().Be("liquid.water");
    }

    [Fact]
    public void ClearContainedLiquid_ShouldRejectAlreadyEmptyItem()
    {
        var item = PlayerPermanentItem.Create("item.flask", null, DateTimeOffset.UtcNow);

        var act = item.ClearContainedLiquid;

        act.Should().Throw<DomainException>()
            .WithMessage("*already empty*");
    }

    [Fact]
    public void SetAndClearContainedLiquid_ShouldNormalizeAndPersistState()
    {
        var item = PlayerPermanentItem.Create("  item.flask  ", Guid.NewGuid(), DateTimeOffset.UtcNow);

        item.SetContainedLiquid("  liquid.water  ");
        item.ContainedLiquidDefinitionKey.Should().Be("liquid.water");

        item.ClearContainedLiquid();
        item.ContainedLiquidDefinitionKey.Should().BeNull();
        item.ItemDefinitionKey.Should().Be("item.flask");
    }

    [Fact]
    public void Rehydrate_ShouldRestoreContainedLiquid()
    {
        var runId = Guid.NewGuid();
        var acquiredAt = DateTimeOffset.UtcNow;

        var item = PlayerPermanentItem.Rehydrate("item.flask", runId, acquiredAt, "liquid.tea");

        item.ItemDefinitionKey.Should().Be("item.flask");
        item.SourceRunId.Should().Be(runId);
        item.AcquiredAtUtc.Should().Be(acquiredAt);
        item.ContainedLiquidDefinitionKey.Should().Be("liquid.tea");
    }
}
