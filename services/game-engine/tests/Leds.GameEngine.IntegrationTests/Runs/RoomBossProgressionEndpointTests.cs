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
        startRunResponse.Run.CurrentRoom.BossPreview.Should().BeNull(
            because: "the authored Hall is a free-exploration social room, not a forced boss room");

        var candidates = startRunResponse.Run.CurrentRoom.Nodes
            .Where(node => node.State == "Available" && node.Type is not "Exit")
            .OrderBy(node => node.IsInitial ? 0 : 1)
            .ThenBy(node => node.ContactBehavior == "None" ? 0 : 1)
            .ToArray();

        candidates.Should().NotBeEmpty(
            because: "the Hall must expose at least one playable encounter before progression");

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
}
