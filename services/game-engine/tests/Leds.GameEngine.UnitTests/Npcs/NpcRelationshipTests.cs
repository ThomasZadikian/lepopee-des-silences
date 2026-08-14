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

    [Fact]
    public void GetAxisScore_ShouldDefaultToZero_ForAnUntouchedAxis()
    {
        var relationship = NpcRelationship.Begin("npc.mane", entryNodeKey: null);

        relationship.GetAxisScore(RelationshipAxis.Trust).Should().Be(0);
    }

    [Fact]
    public void AdjustAxisScore_ShouldAccumulateIndependently_PerAxis()
    {
        var relationship = NpcRelationship.Begin("npc.mane", entryNodeKey: null);

        relationship.AdjustAxisScore(RelationshipAxis.Trust, 5);
        relationship.AdjustAxisScore(RelationshipAxis.Suspicion, 2);
        relationship.AdjustAxisScore(RelationshipAxis.Trust, -1);

        relationship.GetAxisScore(RelationshipAxis.Trust).Should().Be(4);
        relationship.GetAxisScore(RelationshipAxis.Suspicion).Should().Be(2);
        relationship.GetAxisScore(RelationshipAxis.Respect).Should().Be(0);
    }

    [Fact]
    public void AdjustAxisScore_ShouldNeverDecrease_ForElise()
    {
        var relationship = NpcRelationship.Begin("npc.elise", entryNodeKey: null);

        relationship.AdjustAxisScore(RelationshipAxis.Trust, 5);
        relationship.AdjustAxisScore(RelationshipAxis.Trust, -100);

        relationship.GetAxisScore(RelationshipAxis.Trust).Should().Be(5);
    }

    [Fact]
    public void AdjustAxisScore_ShouldNotAffectRelationshipScore_OrWoundStates()
    {
        var relationship = NpcRelationship.Begin("npc.mane", entryNodeKey: null);
        relationship.AdjustScore(3);

        relationship.AdjustAxisScore(RelationshipAxis.Proximity, 7);

        relationship.RelationshipScore.Should().Be(3);
        relationship.AggregateState.Should().Be(WoundState.Latent);
    }

    [Fact]
    public void Remember_ShouldAddTheMemory()
    {
        var relationship = NpcRelationship.Begin("npc.mane", entryNodeKey: null);

        relationship.Remember(NpcMemoryEntry.Create("fact.player-lied-once", MemoryScope.Run, MemoryProvenance.Observed));

        relationship.Memories.Should().ContainSingle(m => m.KnowledgeKey == "fact.player-lied-once");
    }

    [Fact]
    public void ForgetScope_ShouldOnlyRemoveMemoriesOfThatScope()
    {
        var relationship = NpcRelationship.Begin("npc.mane", entryNodeKey: null);
        relationship.Remember(NpcMemoryEntry.Create("fact.a", MemoryScope.Conversation, MemoryProvenance.Observed));
        relationship.Remember(NpcMemoryEntry.Create("fact.b", MemoryScope.Run, MemoryProvenance.PlayerStated));

        relationship.ForgetScope(MemoryScope.Conversation);

        relationship.Memories.Should().ContainSingle(m => m.KnowledgeKey == "fact.b");
    }

    [Fact]
    public void Rehydrate_ShouldPreserveAxisScoresAndMemories()
    {
        var relationship = NpcRelationship.Begin("npc.mane", entryNodeKey: null);
        relationship.AdjustAxisScore(RelationshipAxis.Trust, 4);
        relationship.Remember(NpcMemoryEntry.Create("fact.a", MemoryScope.Run, MemoryProvenance.Rumor));

        var rehydrated = NpcRelationship.Rehydrate(
            relationship.NpcKey,
            relationship.RelationshipScore,
            relationship.WoundStates,
            relationship.Flags,
            relationship.TimesMet,
            relationship.CurrentDialogueNodeKey,
            relationship.AxisScores,
            relationship.Memories);

        rehydrated.GetAxisScore(RelationshipAxis.Trust).Should().Be(4);
        rehydrated.Memories.Should().ContainSingle(m => m.KnowledgeKey == "fact.a");
    }
}
