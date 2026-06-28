using FluentAssertions;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Npcs;

public sealed class NpcTransgressionTests
{
    [Fact]
    public void Create_ShouldCreateTransgression_WithAllProperties()
    {
        var transgression = new NpcTransgression(
            WoundKey: "wound-betrayal",
            TriggerFlag: "player-lied",
            RelationshipPenalty: -10);

        transgression.WoundKey.Should().Be("wound-betrayal");
        transgression.TriggerFlag.Should().Be("player-lied");
        transgression.RelationshipPenalty.Should().Be(-10);
    }

    [Fact]
    public void Create_ShouldCreateTransgression_WithDefaultPenalty_WhenNotProvided()
    {
        var transgression = new NpcTransgression(
            WoundKey: "wound-betrayal",
            TriggerFlag: "player-lied");

        transgression.RelationshipPenalty.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldCreateTransgression_WithPositivePenalty()
    {
        var transgression = new NpcTransgression(
            WoundKey: "wound-healing",
            TriggerFlag: "player-comforted",
            RelationshipPenalty: 5);

        transgression.RelationshipPenalty.Should().Be(5);
    }

    [Fact]
    public void RecordEquality_ShouldBeEqual_WhenAllPropertiesMatch()
    {
        var t1 = new NpcTransgression("wound-key", "trigger", -5);
        var t2 = new NpcTransgression("wound-key", "trigger", -5);

        t1.Should().Be(t2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenWoundKeyDiffers()
    {
        var t1 = new NpcTransgression("wound-key-1", "trigger", -5);
        var t2 = new NpcTransgression("wound-key-2", "trigger", -5);

        t1.Should().NotBe(t2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenTriggerFlagDiffers()
    {
        var t1 = new NpcTransgression("wound-key", "trigger-1", -5);
        var t2 = new NpcTransgression("wound-key", "trigger-2", -5);

        t1.Should().NotBe(t2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenPenaltyDiffers()
    {
        var t1 = new NpcTransgression("wound-key", "trigger", -5);
        var t2 = new NpcTransgression("wound-key", "trigger", -10);

        t1.Should().NotBe(t2);
    }
}
