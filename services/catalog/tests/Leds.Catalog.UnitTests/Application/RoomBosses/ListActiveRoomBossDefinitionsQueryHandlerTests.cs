using FluentAssertions;
using Leds.Catalog.Application.RoomBosses.Dtos;
using Leds.Catalog.Application.RoomBosses.ListActiveRoomBossDefinitions;
using Leds.Catalog.Application.RoomBosses.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.RoomBosses;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.RoomBosses;

public sealed class ListActiveRoomBossDefinitionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedDefinitions_WhenDefinitionsExist()
    {
        var definition = RoomBossDefinition.Create(
            "boss.threshold.warden",
            "Warden of the Threshold",
            "The first sentinel.",
            "1.0.0",
            "Threshold",
            baseDifficulty: 1,
            tags: new[] { "sentinel", "guardian" },
            status: CatalogContentStatus.Active);

        var readStore = Substitute.For<IRoomBossDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IRoomBossDefinition>>(new[] { definition }));

        var handler = new ListActiveRoomBossDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveRoomBossDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().ContainSingle();

        var dto = response.Definitions.Single();
        dto.Key.Should().Be("boss.threshold.warden");
        dto.Name.Should().Be("Warden of the Threshold");
        dto.Status.Should().Be("Active");
        dto.RoomType.Should().Be("Threshold");
        dto.BaseDifficulty.Should().Be(1);
        dto.Tags.Should().BeEquivalentTo("sentinel", "guardian");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoDefinitionsExist()
    {
        var readStore = Substitute.For<IRoomBossDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IRoomBossDefinition>>(Array.Empty<IRoomBossDefinition>()));

        var handler = new ListActiveRoomBossDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveRoomBossDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnMultipleDefinitions_WhenMultipleExist()
    {
        var boss1 = RoomBossDefinition.Create(
            "boss.threshold.warden",
            "Warden",
            "Desc",
            "1.0.0",
            "Threshold",
            baseDifficulty: 1,
            tags: Array.Empty<string>(),
            status: CatalogContentStatus.Active);

        var boss2 = RoomBossDefinition.Create(
            "boss.memory.keeper",
            "Keeper",
            "Desc",
            "1.0.0",
            "Memory",
            baseDifficulty: 2,
            tags: Array.Empty<string>(),
            status: CatalogContentStatus.Active);

        var readStore = Substitute.For<IRoomBossDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IRoomBossDefinition>>(new[] { boss1, boss2 }));

        var handler = new ListActiveRoomBossDefinitionsQueryHandler(readStore);

        var response = await handler.Handle(
            new ListActiveRoomBossDefinitionsQuery(),
            CancellationToken.None);

        response.Definitions.Should().HaveCount(2);
        response.Definitions.Should().Contain(d => d.Key == "boss.threshold.warden");
        response.Definitions.Should().Contain(d => d.Key == "boss.memory.keeper");
    }
}
