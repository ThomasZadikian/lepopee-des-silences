using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Runs.InteractWithRoomNpc;
using Leds.GameEngine.Application.Protocol;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunActorInteractionTests
{
    [Fact]
    public async Task InteractWithRoomNpc_ShouldBridgeAwarenessAndRunDialogueIdentity()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Npc).Run;
        var room = run.CurrentRoom;
        var (npcX, npcY) = AdjacentFloorCell(room.Grid);
        var npc = RoomNpc.Create(
            "npc.majordome",
            npcX,
            npcY,
            NpcBehaviorArchetype.Fixed);
        room.AddRoomNpc(npc);

        var repository = new Mock<IRunRepository>();
        repository.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        var handler = new InteractWithRoomNpcCommandHandler(
            repository.Object,
            new LocalRuleProtocolEvaluator(Mock.Of<ILocalRuleProvider>()));

        var response = await handler.Handle(
            new InteractWithRoomNpcCommand(run.Id.Value, npc.Id.Value),
            CancellationToken.None);

        npc.Awareness.Should().Be(NpcAwarenessState.Aware);
        run.ActiveNpcKey.Should().Be("npc.majordome");
        response.Actor.ActorKind.Should().Be("Npc");
        repository.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (int X, int Y) AdjacentFloorCell(RoomGrid grid)
    {
        var candidates = new[]
        {
            (grid.PartyX + 1, grid.PartyY),
            (grid.PartyX - 1, grid.PartyY),
            (grid.PartyX, grid.PartyY + 1),
            (grid.PartyX, grid.PartyY - 1)
        };

        return candidates.First(cell =>
            cell.Item1 >= 0 && cell.Item1 < grid.Width
            && cell.Item2 >= 0 && cell.Item2 < grid.Height
            && grid.IsFloor(cell.Item1, cell.Item2));
    }
}
