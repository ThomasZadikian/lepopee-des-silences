using System.Reflection;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.ConfirmRoomExit;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs.ConfirmRoomExit;

public sealed class ConfirmRoomExitBranchCoverageTests
{
    private static readonly MethodInfo HasResolvedCombatNodeMethod =
        typeof(ConfirmRoomExitCommandHandler).GetMethod(
            "HasResolvedCombatNode",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("HasResolvedCombatNode was not found.");

    [Theory]
    [InlineData(NodeEventType.Combat, true)]
    [InlineData(NodeEventType.Elite, true)]
    [InlineData(NodeEventType.RoomBoss, true)]
    [InlineData(NodeEventType.FinalBoss, true)]
    [InlineData(NodeEventType.Item, false)]
    [InlineData(NodeEventType.Npc, false)]
    [InlineData(NodeEventType.Rest, false)]
    public void HasResolvedCombatNode_ShouldRecognizeOnlyResolvedCombatFamilies(
        NodeEventType eventType,
        bool expected)
    {
        var fixture = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(eventType);

        HasResolvedCombatNode(fixture.Run.CurrentRoom).Should().Be(expected);
    }

    [Fact]
    public void HasResolvedCombatNode_ShouldIgnoreUnresolvedCombatNodes()
    {
        var run = TestGameEngineFactory.CreateRun();

        HasResolvedCombatNode(run.CurrentRoom).Should().BeFalse();
    }

    private static bool HasResolvedCombatNode(Room room) =>
        (bool)HasResolvedCombatNodeMethod.Invoke(null, [room])!;
}
