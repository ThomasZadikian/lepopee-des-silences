using FluentAssertions;
using Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByKey;
using Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByRoomType;
using Leds.Catalog.Application.RoomBosses.ListActiveRoomBossDefinitions;
using Leds.Catalog.Application.RoomBosses.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.RoomBosses;
using NSubstitute;

namespace Leds.Catalog.UnitTests.Application.RoomBosses;

public sealed class RoomBossDefinitionQueryHandlerTests
{
    [Fact]
    public async Task GetByKey_ShouldReturnDefinition_WhenDefinitionExists()
    {
        var definition = CreateRoomBossDefinition();

        var readStore = Substitute.For<IRoomBossDefinitionReadStore>();
        readStore.GetByKeyAsync("boss.threshold.warden", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IRoomBossDefinition?>(definition));

        var handler = new GetRoomBossDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetRoomBossDefinitionByKeyQuery("boss.threshold.warden"),
            CancellationToken.None);

        response.Definition.Should().NotBeNull();

        response.Definition!.Key.Should().Be("boss.threshold.warden");
        response.Definition.Name.Should().Be("Warden of the Threshold");
        response.Definition.Version.Should().Be("1.0.0");
        response.Definition.Status.Should().Be("Active");
        response.Definition.RoomType.Should().Be("Threshold");
        response.Definition.BaseDifficulty.Should().Be(1);
        response.Definition.Tags.Should().BeEquivalentTo("sentinel", "guardian");
    }

    [Fact]
    public async Task GetByKey_ShouldReturnNullDefinition_WhenDefinitionDoesNotExist()
    {
        var readStore = Substitute.For<IRoomBossDefinitionReadStore>();
        readStore.GetByKeyAsync("unknown-boss", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IRoomBossDefinition?>(null));

        var handler = new GetRoomBossDefinitionByKeyQueryHandler(readStore);

        var response = await handler.Handle(
            new GetRoomBossDefinitionByKeyQuery("unknown-boss"),
            CancellationToken.None);

        response.Definition.Should().BeNull();
    }

    [Fact]
    public async Task GetByRoomType_ShouldReturnDefinition_WhenRoomTypeExists()
    {
        var definition = CreateRoomBossDefinition();

        var readStore = Substitute.For<IRoomBossDefinitionReadStore>();
        readStore.GetByRoomTypeAsync("Threshold", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IRoomBossDefinition?>(definition));

        var handler = new GetRoomBossDefinitionByRoomTypeQueryHandler(readStore);

        var response = await handler.Handle(
            new GetRoomBossDefinitionByRoomTypeQuery("Threshold"),
            CancellationToken.None);

        response.Definition.Should().NotBeNull();

        response.Definition!.RoomType.Should().Be("Threshold");
        response.Definition.BaseDifficulty.Should().Be(1);
    }

    [Fact]
    public async Task GetByRoomType_ShouldReturnNullDefinition_WhenRoomTypeDoesNotExist()
    {
        var readStore = Substitute.For<IRoomBossDefinitionReadStore>();
        readStore.GetByRoomTypeAsync("Unknown", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IRoomBossDefinition?>(null));

        var handler = new GetRoomBossDefinitionByRoomTypeQueryHandler(readStore);

        var response = await handler.Handle(
            new GetRoomBossDefinitionByRoomTypeQuery("Unknown"),
            CancellationToken.None);

        response.Definition.Should().BeNull();
    }

    [Fact]
    public async Task ListActive_ShouldReturnMappedDefinitions()
    {
        var definition = CreateRoomBossDefinition();

        var readStore = Substitute.For<IRoomBossDefinitionReadStore>();
        readStore.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<IRoomBossDefinition>>(
                new[] { definition }));

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

    private static RoomBossDefinition CreateRoomBossDefinition()
    {
        var definition = RoomBossDefinition.Create(
            "boss.threshold.warden",
            "Warden of the Threshold",
            "The first sentinel guarding the entrance.",
            "1.0.0",
            "Threshold",
            baseDifficulty: 1,
            tags: ["sentinel", "guardian"],
            status: CatalogContentStatus.Draft);

        definition.Activate();

        return definition;
    }
}
