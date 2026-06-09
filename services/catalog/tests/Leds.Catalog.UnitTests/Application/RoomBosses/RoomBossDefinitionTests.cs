using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.RoomBosses;

namespace Leds.Catalog.UnitTests.Application.RoomBosses;

public sealed class RoomBossDefinitionTests
{
    [Fact]
    public void Create_ShouldCreateRoomBossDefinition_WhenInputIsValid()
    {
        var definition = RoomBossDefinition.Create(
            "boss.threshold.warden",
            "Warden of the Threshold",
            "The first sentinel guarding the entrance.",
            "1.0.0",
            "Threshold",
            baseDifficulty: 1,
            tags: ["sentinel", "guardian"]);

        definition.Id.Value.Should().NotBeEmpty();
        definition.Key.Value.Should().Be("boss.threshold.warden");
        definition.Name.Value.Should().Be("Warden of the Threshold");
        definition.Description.Value.Should().Be("The first sentinel guarding the entrance.");
        definition.Version.Value.Should().Be("1.0.0");
        definition.Status.Should().Be(CatalogContentStatus.Draft);
        definition.RoomType.Should().Be("Threshold");
        definition.BaseDifficulty.Should().Be(1);
        definition.Tags.Should().BeEquivalentTo("sentinel", "guardian");
    }

    [Fact]
    public void Create_ShouldRemoveDuplicateTags()
    {
        var definition = RoomBossDefinition.Create(
            "boss.threshold.warden",
            "Warden",
            "Description",
            "1.0.0",
            "Threshold",
            baseDifficulty: 1,
            tags: ["guardian", "guardian"]);

        definition.Tags.Should().ContainSingle();
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenDescriptionIsEmpty()
    {
        var act = () => RoomBossDefinition.Create(
            "boss.threshold.warden",
            "Warden",
            null,
            "1.0.0",
            "Threshold",
            baseDifficulty: 1,
            tags: []);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Room boss definition description is required.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenRoomTypeIsEmpty()
    {
        var act = () => RoomBossDefinition.Create(
            "boss.threshold.warden",
            "Warden",
            "Description",
            "1.0.0",
            string.Empty,
            baseDifficulty: 1,
            tags: []);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Room boss definition room type is required.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenBaseDifficultyIsZero()
    {
        var act = () => RoomBossDefinition.Create(
            "boss.threshold.warden",
            "Warden",
            "Description",
            "1.0.0",
            "Threshold",
            baseDifficulty: 0,
            tags: []);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Room boss definition base difficulty must be greater than 0.");
    }

    [Fact]
    public void Create_ShouldHandleNullTags()
    {
        var definition = RoomBossDefinition.Create(
            "boss.threshold.warden",
            "Warden",
            "Description",
            "1.0.0",
            "Threshold",
            baseDifficulty: 1,
            tags: null);

        definition.Tags.Should().BeEmpty();
    }
}
