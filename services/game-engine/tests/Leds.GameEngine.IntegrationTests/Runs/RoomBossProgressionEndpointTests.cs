using FluentAssertions;
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

        // Hall progression must not depend on the random content mix containing an Item or
        // Rare node. Use the nearest reachable explicitly-confirmable encounter exposed by
        // the room contract; ResolveAndHandleCombatAsync already handles event choices and
        // any combat that the selected encounter may legitimately start.
        var encounter = FirstConfirmableNode(room);
        await MovePartyAndEnterNodeAsync(runId, encounter);

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
}
