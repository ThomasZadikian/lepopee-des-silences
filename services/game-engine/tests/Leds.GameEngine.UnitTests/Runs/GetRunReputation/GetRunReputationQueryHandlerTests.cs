using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.GetRunReputation;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GetRunReputation;

public sealed class GetRunReputationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoNpcHasBeenMet()
    {
        var run = TestGameEngineFactory.CreateRun();

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var handler = new GetRunReputationQueryHandler(runRepo.Object, catalogGateway.Object);

        var response = await handler.Handle(new GetRunReputationQuery(run.Id.Value), CancellationToken.None);

        response.RunId.Should().Be(run.Id.Value);
        response.Npcs.Should().BeEmpty();
        catalogGateway.Verify(
            g => g.ListNpcDefinitionsAsync(It.IsAny<CancellationToken>()), Times.Never,
            "no relationship exists yet, so the catalog should not even be queried.");
    }

    [Fact]
    public async Task Handle_ShouldJoinRelationshipWithCatalogData()
    {
        var run = TestGameEngineFactory.CreateRun();
        var relationship = run.BeginOrResumeNpcEncounter("npc.erina");
        relationship.AdjustScore(300);

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var offerings = new[]
        {
            new CatalogNpcOffering("offer.erina.reve", "Item", "canon.item.reve-erina", 1, true,
                new[] { new CatalogDialogueRequirement("RelationshipScoreAtLeast", null, null, null, RequiredRelationshipScore: 250) }),
            new CatalogNpcOffering("offer.erina.liberte", "Skill", "canon.skill.liberte-retrouvee", 1, true,
                new[] { new CatalogDialogueRequirement("RelationshipScoreAtLeast", null, null, null, RequiredRelationshipScore: 1000) })
        };

        var npc = new CatalogNpcDefinition(
            "npc.erina", "Erina", "Une adolescente arrogante.",
            Tags: [], CompatibleRoomTypes: [], CompatiblePalaceRoomStates: [], CompatibleRoomClimates: [],
            EmotionalAffinity: "Rupture", IsRecurring: true, Offerings: offerings);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.ListNpcDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { npc });

        var handler = new GetRunReputationQueryHandler(runRepo.Object, catalogGateway.Object);

        var response = await handler.Handle(new GetRunReputationQuery(run.Id.Value), CancellationToken.None);

        response.Npcs.Should().ContainSingle();
        var dto = response.Npcs.Single();
        dto.NpcKey.Should().Be("npc.erina");
        dto.DisplayName.Should().Be("Erina");
        dto.EmotionalRegister.Should().Be("Rupture");
        dto.RelationshipScore.Should().Be(300);
        dto.AggregateState.Should().Be("Latent");
        dto.TimesMet.Should().Be(1);
        dto.Offerings.Should().HaveCount(2);
        dto.Offerings.Should().Contain(o => o.Key == "offer.erina.reve" && o.ScoreThresholdMet);
        dto.Offerings.Should().Contain(o => o.Key == "offer.erina.liberte" && !o.ScoreThresholdMet);
    }

    [Fact]
    public async Task Handle_ShouldSkipRelationships_WhenNpcIsNotInCatalog()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.BeginOrResumeNpcEncounter("npc.unknown");

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.ListNpcDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetRunReputationQueryHandler(runRepo.Object, catalogGateway.Object);

        var response = await handler.Handle(new GetRunReputationQuery(run.Id.Value), CancellationToken.None);

        response.Npcs.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(new RunId(runId), It.IsAny<CancellationToken>())).ReturnsAsync((Run?)null);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var handler = new GetRunReputationQueryHandler(runRepo.Object, catalogGateway.Object);

        var act = () => handler.Handle(new GetRunReputationQuery(runId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Run*");
    }
}
