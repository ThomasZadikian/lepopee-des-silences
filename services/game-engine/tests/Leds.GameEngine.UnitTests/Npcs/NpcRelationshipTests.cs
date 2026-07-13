using FluentAssertions;
using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.UnitTests.Npcs;

public sealed class NpcRelationshipTests
{
    [Fact]
    public void AdjustScore_ShouldIncreaseAndDecrease_ForAnOrdinaryNpc()
    {
        var relationship = NpcRelationship.Begin("npc.mane", entryNodeKey: null);

        relationship.AdjustScore(10);
        relationship.AdjustScore(-4);

        relationship.RelationshipScore.Should().Be(6);
    }

    [Fact]
    public void AdjustScore_ShouldNeverDecrease_ForElise()
    {
        var relationship = NpcRelationship.Begin("npc.elise", entryNodeKey: null);

        relationship.AdjustScore(10);
        relationship.AdjustScore(-100);

        relationship.RelationshipScore.Should().Be(10,
            because: "Elise is totally apathetic — her reputation can never decrease.");
    }

    [Fact]
    public void AdjustScore_ShouldStillIncrease_ForElise()
    {
        var relationship = NpcRelationship.Begin("npc.elise", entryNodeKey: null);

        relationship.AdjustScore(5);
        relationship.AdjustScore(5);

        relationship.RelationshipScore.Should().Be(10);
    }

    [Fact]
    public void AdjustScore_ShouldBeCaseInsensitive_ForEliseKey()
    {
        var relationship = NpcRelationship.Begin("NPC.ELISE", entryNodeKey: null);

        relationship.AdjustScore(10);
        relationship.AdjustScore(-10);

        relationship.RelationshipScore.Should().Be(10);
    }
}
