using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Application.Runs.ProgressRun;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

[Collection("GameEngineApi")]
public sealed class RoomBossProgressionEndpointTests : RunIntegrationTestBase
{
    public RoomBossProgressionEndpointTests(GameEngineApiFactory factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task RoomProgression_ShouldRemainPlayable_WithoutAuthoredHallBoss()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var room = startRunResponse.Run.CurrentRoom;
        room.BossPreview.Should().BeNull(
            because: "the authored Hall is a free-exploration social room, not a forced boss room");

        var candidates = room.Nodes
            .Where(node => node.State == "Available" && node.Type is not "Exit")
            .Where(node => HasSafePath(room, node))
            .OrderBy(node => node.IsInitial ? 0 : 1)
            .ThenBy(node => node.ContactBehavior == "None" ? 0 : 1)
            .ToArray();

        candidates.Should().NotBeEmpty(
            because: "the Hall must expose at least one reachable playable encounter before progression");

        var encounter = candidates[0];
        if (encounter.ContactBehavior == "None")
        {
            await MovePartyAndEnterNodeAsync(runId, encounter);
        }
        else
        {
            await MovePartyToNodeAsync(runId, encounter);
        }

        var resolvedPayload = await ResolveAndHandleCombatAsync(runId);
        resolvedPayload.Run.Status.Should().Be("Active");
        resolvedPayload.Run.CurrentRoom.State.Should().Be("NodeResolved");

        var progressResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/progress",
            content: null);
        var progressBody = await progressResponse.Content.ReadAsStringAsync();
        progressResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: progressBody);

        var progressed = await progressResponse.Content.ReadFromJsonAsync<ProgressRunResponse>();
        progressed.Should().NotBeNull();
        progressed!.Run.Status.Should().Be("Active");
        progressed.Run.CurrentRoom.State.Should().Be("Active");
        progressed.Run.CurrentRoom.Nodes.Single(node => node.Id == encounter.Id)
            .State.Should().Be("Resolved");
    }

    private static bool HasSafePath(RoomDto room, MapNodeDto target)
    {
        var grid = room.Grid!;
        var start = (X: grid.PartyX, Y: grid.PartyY);
        var destination = (X: target.Lane, Y: target.Row);
        if (start == destination)
        {
            return false;
        }

        var obstacles = grid.ObstacleCells
            .Select(cell => (X: cell[0], Y: cell[1]))
            .ToHashSet();
        var triggers = room.Nodes
            .Where(node =>
                node.Id != target.Id
                && node.State == "Available"
                && node.ContactBehavior is "TriggerOnEnter" or "Blocking")
            .Select(node => (X: node.Lane, Y: node.Row))
            .ToHashSet();

        var queue = new Queue<(int X, int Y)>();
        var visited = new HashSet<(int X, int Y)> { start };
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == destination)
            {
                return true;
            }

            foreach (var next in new[]
                     {
                         (X: current.X + 1, Y: current.Y),
                         (X: current.X - 1, Y: current.Y),
                         (X: current.X, Y: current.Y + 1),
                         (X: current.X, Y: current.Y - 1)
                     })
            {
                if (next.X < 0 || next.X >= grid.Width
                    || next.Y < 0 || next.Y >= grid.Height)
                {
                    continue;
                }

                var index = (next.Y * grid.Width) + next.X;
                if (!grid.FloorCells[index]
                    || obstacles.Contains(next)
                    || triggers.Contains(next)
                    || !visited.Add(next))
                {
                    continue;
                }

                queue.Enqueue(next);
            }
        }

        return false;
    }
}
