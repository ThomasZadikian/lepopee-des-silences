using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Protocol;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Rooms;

public sealed class RoomLocalRuleStateTests
{
    private static RoomBossProfile CreateBossProfile() => RoomBossProfile.Create(
        bossId: "boss.test.rule", name: "Gardien de Test", roomType: RoomType.Threshold,
        dangerHint: "High", enemyTemplateKey: "boss-test-rule-v1");

    private static MapNode CreateAvailableNode(int lane, int row, bool isBoss = false) => MapNode.Create(
        isBoss ? NodeEventType.RoomBoss : NodeEventType.Item,
        riskLevel: isBoss ? 85 : 10,
        rewardProfile: isBoss ? "room-boss" : "standard",
        row, lane, parentNodeIds: Array.Empty<NodeId>(),
        isBoss: isBoss, initialState: NodeState.Available);

    private static Room CreateRoom(int movementBudget = 20)
    {
        var bossNode = CreateAvailableNode(lane: 9, row: 9, isBoss: true);

        return Room.Create(
            depth: 0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold",
            CreateBossProfile(), [bossNode],
            gridWidth: 10, gridHeight: 10, movementBudget, startX: 0, startY: 0,
            layoutTemplateKey: "test-rule-v1", layoutTemplateVersion: "1.0.0");
    }

    [Fact]
    public void AddLocalRuleState_ShouldRegisterIt()
    {
        var room = CreateRoom();
        var state = LocalRuleState.Create("rule.hall.tapis");

        room.AddLocalRuleState(state);

        room.LocalRuleStates.Should().ContainSingle(s => s.LocalRuleKey == "rule.hall.tapis");
    }

    [Fact]
    public void AddLocalRuleState_ShouldRejectADuplicateKey()
    {
        var room = CreateRoom();
        room.AddLocalRuleState(LocalRuleState.Create("rule.hall.tapis"));

        var act = () => room.AddLocalRuleState(LocalRuleState.Create("rule.hall.tapis"));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GetLocalRuleState_ShouldReturnNull_WhenNeverTracked()
    {
        var room = CreateRoom();

        room.GetLocalRuleState("rule.unknown").Should().BeNull();
    }

    [Fact]
    public void GetLocalRuleState_ShouldReturnTheMatchingTrackedState()
    {
        var room = CreateRoom();
        var state = LocalRuleState.Create("rule.hall.tapis");
        room.AddLocalRuleState(state);

        room.GetLocalRuleState("rule.hall.tapis").Should().BeSameAs(state);
    }
}
