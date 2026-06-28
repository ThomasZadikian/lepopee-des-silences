using FluentAssertions;
using Leds.Catalog.Application.RewardTemplates.Dtos;
using Leds.Catalog.Application.RewardTemplates.ListEligibleRewardTemplates;
using Leds.Catalog.Application.RewardTemplates.Ports;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.RewardTemplates;

public sealed class ListEligibleRewardTemplatesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEligibleTemplates_WhenTemplatesAreAvailable()
    {
        var dto = new RewardTemplateDto(
            Id: Guid.NewGuid(),
            Key: "reward.healing-pool",
            Version: "catalog-0.1.0",
            DisplayName: "Healing Pool",
            Description: "A pool that heals the player.",
            SourceType: "enemy",
            MinChoices: 1,
            MaxChoices: 3,
            Status: "Active",
            Options: Array.Empty<RewardTemplateOptionDto>());

        var readStore = Substitute.For<IRewardTemplateReadStore>();
        readStore.ListEligibleDtosAsync(
            Arg.Any<RewardTemplateEligibilityQueryContext>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<RewardTemplateDto>>(new[] { dto }));

        var handler = new ListEligibleRewardTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListEligibleRewardTemplatesQuery(
                SourceType: "enemy",
                Depth: 2,
                CombatTier: "normal",
                DifficultyMultiplier: 1.0,
                RewardPowerMultiplier: 1.5),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();
        response.Definitions.Single().Key.Should().Be("reward.healing-pool");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoTemplatesAreEligible()
    {
        var readStore = Substitute.For<IRewardTemplateReadStore>();
        readStore.ListEligibleDtosAsync(
            Arg.Any<RewardTemplateEligibilityQueryContext>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<RewardTemplateDto>>(Array.Empty<RewardTemplateDto>()));

        var handler = new ListEligibleRewardTemplatesQueryHandler(readStore);

        var response = await handler.Handle(
            new ListEligibleRewardTemplatesQuery(
                SourceType: "enemy",
                Depth: null,
                CombatTier: null,
                DifficultyMultiplier: null,
                RewardPowerMultiplier: null),
            CancellationToken.None);

        response.Definitions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectContext_ToReadStore()
    {
        RewardTemplateEligibilityQueryContext? capturedContext = null;

        var readStore = Substitute.For<IRewardTemplateReadStore>();
        readStore.ListEligibleDtosAsync(
            Arg.Do<RewardTemplateEligibilityQueryContext>(ctx => capturedContext = ctx),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<RewardTemplateDto>>(Array.Empty<RewardTemplateDto>()));

        var handler = new ListEligibleRewardTemplatesQueryHandler(readStore);

        await handler.Handle(
            new ListEligibleRewardTemplatesQuery(
                SourceType: "boss",
                Depth: 3,
                CombatTier: "hard",
                DifficultyMultiplier: 2.0,
                RewardPowerMultiplier: 1.5),
            CancellationToken.None);

        capturedContext.Should().NotBeNull();
        capturedContext!.SourceType.Should().Be("boss");
        capturedContext.Depth.Should().Be(3);
        capturedContext.CombatTier.Should().Be("hard");
        capturedContext.DifficultyMultiplier.Should().Be(2.0);
        capturedContext.RewardPowerMultiplier.Should().Be(1.5);
    }
}
