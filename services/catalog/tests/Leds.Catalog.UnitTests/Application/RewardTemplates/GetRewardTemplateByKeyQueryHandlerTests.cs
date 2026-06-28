using FluentAssertions;
using Leds.Catalog.Application.RewardTemplates.Dtos;
using Leds.Catalog.Application.RewardTemplates.GetRewardTemplateByKey;
using Leds.Catalog.Application.RewardTemplates.Ports;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.RewardTemplates;

public sealed class GetRewardTemplateByKeyQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnTemplate_WhenTemplateExists()
    {
        var dto = new RewardTemplateDto(
            Id: Guid.NewGuid(),
            Key: "reward.healing-pool",
            Version: "catalog-0.1.0",
            DisplayName: "Healing Pool",
            Description: "A pool that heals.",
            SourceType: "enemy",
            MinChoices: 1,
            MaxChoices: 3,
            Status: "Active",
            Options: new[]
            {
                new RewardTemplateOptionDto(
                    RewardType: "Heal",
                    Label: "Heal 10",
                    Description: "Restore 10 HP",
                    PayloadKey: null,
                    PayloadType: null,
                    EffectSetKey: null,
                    BaseAmount: 10,
                    ScalingMode: "Fixed",
                    Weight: 100)
            });

        var readStore = Substitute.For<IRewardTemplateReadStore>();
        readStore.GetDtoByKeyAsync("reward.healing-pool", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RewardTemplateDto?>(dto));

        var handler = new GetRewardTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetRewardTemplateByKeyQuery("reward.healing-pool"),
            CancellationToken.None);

        response.Definition.Should().NotBeNull();
        response.Definition!.Key.Should().Be("reward.healing-pool");
        response.Definition.DisplayName.Should().Be("Healing Pool");
        response.Definition.Options.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ShouldReturnNullDefinition_WhenTemplateDoesNotExist()
    {
        var readStore = Substitute.For<IRewardTemplateReadStore>();
        readStore.GetDtoByKeyAsync("unknown-reward", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RewardTemplateDto?>(null));

        var handler = new GetRewardTemplateByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetRewardTemplateByKeyQuery("unknown-reward"),
            CancellationToken.None);

        response.Definition.Should().BeNull();
    }
}
