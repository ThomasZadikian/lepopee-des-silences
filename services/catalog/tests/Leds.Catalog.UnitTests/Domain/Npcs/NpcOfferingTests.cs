using FluentAssertions;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Npcs;

public sealed class NpcOfferingTests
{
    [Fact]
    public void Create_ShouldCreateOffering_WithAllProperties()
    {
        var requirement = new DialogueRequirement(DialogueRequirementKind.FlagPresent, FlagKey: "met-once");

        var offering = new NpcOffering(
            Key: "offering.hitomi.skill",
            Kind: NpcOfferingKind.Skill,
            TargetKey: "skill.basic.strike",
            Amount: 0,
            IsMajor: true,
            UnlockConditions: [requirement]);

        offering.Key.Should().Be("offering.hitomi.skill");
        offering.Kind.Should().Be(NpcOfferingKind.Skill);
        offering.TargetKey.Should().Be("skill.basic.strike");
        offering.IsMajor.Should().BeTrue();
        offering.UnlockConditions.Should().ContainSingle().Which.Should().Be(requirement);
    }

    [Fact]
    public void Create_ShouldSupportStatPointOffering_WithNullTargetKey()
    {
        var offering = new NpcOffering(
            Key: "offering.hitomi.stat-point",
            Kind: NpcOfferingKind.StatPoint,
            TargetKey: null,
            Amount: 1,
            IsMajor: false,
            UnlockConditions: []);

        offering.TargetKey.Should().BeNull();
        offering.Amount.Should().Be(1);
        offering.IsMajor.Should().BeFalse();
        offering.UnlockConditions.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldSupportRelationshipScoreGatedOffering()
    {
        var requirement = new DialogueRequirement(
            DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 5);

        var offering = new NpcOffering(
            Key: "offering.hitomi.chaleur-protectrice",
            Kind: NpcOfferingKind.Skill,
            TargetKey: "skill.hitomi.chaleur-protectrice",
            Amount: 0,
            IsMajor: true,
            UnlockConditions: [requirement]);

        offering.UnlockConditions.Should().ContainSingle();
        var unlockCondition = offering.UnlockConditions.Single();
        unlockCondition.Kind.Should().Be(DialogueRequirementKind.RelationshipScoreAtLeast);
        unlockCondition.RequiredRelationshipScore.Should().Be(5);
    }

    [Fact]
    public void RecordEquality_ShouldBeEqual_WhenAllPropertiesMatch()
    {
        var o1 = new NpcOffering("key", NpcOfferingKind.Item, "item.consumable.minor-heal", 1, false, []);
        var o2 = new NpcOffering("key", NpcOfferingKind.Item, "item.consumable.minor-heal", 1, false, []);

        o1.Should().Be(o2);
    }
}
