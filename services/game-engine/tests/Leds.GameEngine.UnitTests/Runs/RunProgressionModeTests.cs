using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunProgressionModeTests
{
    [Fact]
    public void StoryDifficulty_DifficultyN_AndRiskTier_ShouldRemainSeparateTypes()
    {
        var run = CreateBosslessRun();

        run.ConfigureStoryRun(new StoryRunOverlay("story.main", "1.0", "step", "checkpoint"));

        run.ProgressionMode.Should().Be(RunProgressionMode.Story);
        run.StoryDifficulty.Should().Be(StoryDifficulty.Canonical);
        run.DifficultyLevel.Should().BeNull();

        run.ConfigureDifficultyRun(DifficultyLevel.Create(2));

        run.ProgressionMode.Should().Be(RunProgressionMode.Standard);
        run.StoryDifficulty.Should().BeNull();
        run.DifficultyLevel!.Value.Value.Should().Be(2);
    }

    [Fact]
    public void StartNew_ShouldAllowAnAuthoredRoomWithoutBoss()
    {
        var act = CreateBosslessRun;

        act.Should().NotThrow();
    }

    private static Run CreateBosslessRun()
    {
        var nodes = Enumerable.Range(0, 6)
            .Select(index => MapNode.Create(
                NodeEventType.Item,
                riskLevel: 0,
                rewardProfile: "standard",
                row: index / 3 + 1,
                lane: index % 3 + 1,
                parentNodeIds: [],
                initialState: NodeState.Available))
            .ToArray();
        var room = Room.Create(
            depth: 0,
            RoomType.Threshold,
            PalaceRoomState.Neutral,
            "Threshold",
            bossProfile: null,
            nodes,
            gridWidth: 8,
            gridHeight: 8,
            movementBudget: 0,
            startX: 0,
            startY: 0,
            layoutTemplateKey: "test.threshold",
            layoutTemplateVersion: "1.0");

        return Run.StartNew(
            Guid.NewGuid(),
            "seed",
            "generator",
            "markov",
            room,
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());
    }
}
