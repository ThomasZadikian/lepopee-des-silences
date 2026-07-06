using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Npcs;

public sealed class NpcDefinitionTests
{
    [Fact]
    public void Create_ShouldCreateNpcDefinition_WhenInputIsValid()
    {
        var npc = NpcDefinition.Create(
            "npc-neutral-traveler",
            "Voyageur neutre",
            "Un voyageur sans attache.",
            "alpha-0.8.1",
            tags: ["npc", "generic"],
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: null,
            compatibleRoomClimates: null);

        npc.Id.Value.Should().NotBeEmpty();
        npc.Key.Value.Should().Be("npc-neutral-traveler");
        npc.Name.Value.Should().Be("Voyageur neutre");
        npc.Description.Value.Should().Be("Un voyageur sans attache.");
        npc.Version.Value.Should().Be("alpha-0.8.1");
        npc.Status.Should().Be(CatalogContentStatus.Draft);
        npc.Tags.Should().BeEquivalentTo("npc", "generic");
        npc.CompatibleRoomTypes.Should().BeEmpty();
        npc.CompatiblePalaceRoomStates.Should().BeEmpty();
        npc.CompatibleRoomClimates.Should().BeEmpty();
        npc.MinDepth.Should().BeNull();
        npc.MaxDepth.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCreateNpcDefinition_WithStateAndClimateConstraints()
    {
        var npc = NpcDefinition.Create(
            "npc-silent-witness",
            "Témoin silencieux",
            null,
            "alpha-0.8.1",
            tags: null,
            compatibleRoomTypes: ["Silence"],
            compatiblePalaceRoomStates: ["Silent"],
            compatibleRoomClimates: ["Rain"],
            minDepth: 1,
            maxDepth: 5,
            status: CatalogContentStatus.Active);

        npc.IsActive.Should().BeTrue();
        npc.CompatibleRoomTypes.Should().BeEquivalentTo("Silence");
        npc.CompatiblePalaceRoomStates.Should().BeEquivalentTo("Silent");
        npc.CompatibleRoomClimates.Should().BeEquivalentTo("Rain");
        npc.MinDepth.Should().Be(1);
        npc.MaxDepth.Should().Be(5);
        npc.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldRemoveDuplicateTags()
    {
        var npc = NpcDefinition.Create(
            "npc-test",
            "Test",
            null,
            "1.0.0",
            tags: ["npc", "NPC", "npc"],
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: null,
            compatibleRoomClimates: null);

        npc.Tags.Should().HaveCount(1);
        npc.Tags.Should().Contain("npc");
    }

    [Fact]
    public void Active_ShouldReturnCorrectActiveStatus()
    {
        var npc = NpcDefinition.Create(
            "npc-test",
            "Test",
            null,
            "1.0.0",
            tags: null,
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: null,
            compatibleRoomClimates: null,
            status: CatalogContentStatus.Active);

        npc.IsActive.Should().BeTrue();
        npc.IsDraft.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldDefaultBoundRoomKeysAndOfferingsToEmpty()
    {
        var npc = NpcDefinition.Create(
            "npc-test",
            "Test",
            null,
            "1.0.0",
            tags: null,
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: null,
            compatibleRoomClimates: null);

        npc.BoundRoomKeys.Should().BeEmpty();
        npc.Offerings.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldStoreBoundRoomKeysAndOfferings()
    {
        var offering = new NpcOffering(
            "offering.test.skill", NpcOfferingKind.Skill, "skill.basic.strike", 0, IsMajor: true,
            UnlockConditions: [new DialogueRequirement(DialogueRequirementKind.FlagPresent, FlagKey: "met-once")]);

        var npc = NpcDefinition.Create(
            "npc-bound-test",
            "Test lié",
            null,
            "1.0.0",
            tags: null,
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: null,
            compatibleRoomClimates: null,
            boundRoomKeys: ["room.room08"],
            offerings: [offering]);

        npc.BoundRoomKeys.Should().BeEquivalentTo("room.room08");
        npc.Offerings.Should().ContainSingle().Which.Should().BeEquivalentTo(offering);
    }
}
