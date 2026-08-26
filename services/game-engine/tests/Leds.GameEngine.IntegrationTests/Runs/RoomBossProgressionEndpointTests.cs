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
    public async Task RoomProgression_ShouldEventuallyReachRoomBoss_AndCompleteRoom_WhenBossIsResolved()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var currentRoom = startRunResponse.Run.CurrentRoom;

        while (currentRoom.CurrentNodeDepth < currentRoom.MaxNodeDepth)
        {
            var nodeToChoose = currentRoom.AvailableNodes.First();

            var chooseResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/nodes/{nodeToChoose.Id}/choose",
                content: null);

            var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

            chooseResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: chooseBody);

            var resolvedPayload = await ResolveAndHandleCombatAsync(runId);

            resolvedPayload.Run.Status.Should().Be("Active");
            resolvedPayload.Run.CurrentRoom.State.Should().Be("NodeResolved");

            var progressResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/progress",
                content: null);

            var progressBody = await progressResponse.Content.ReadAsStringAsync();

            progressResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: progressBody);

            var progressPayload = await progressResponse.Content
                .ReadFromJsonAsync<ProgressRunResponse>();

            progressPayload.Should().NotBeNull();

            currentRoom = progressPayload!.Run.CurrentRoom;

            currentRoom.AvailableNodes.Should().NotBeEmpty();
            currentRoom.AvailableNodes.Should().OnlyContain(node =>
                node.Row == currentRoom.CurrentNodeDepth);
        }

        currentRoom.State.Should().Be("BossReached");
        currentRoom.CurrentNodeDepth.Should().Be(currentRoom.MaxNodeDepth);
        currentRoom.AvailableNodes.Should().ContainSingle();

        var bossNode = currentRoom.AvailableNodes.Single();

        bossNode.Type.Should().Be("RoomBoss");
        bossNode.IsBoss.Should().BeTrue();
        bossNode.State.Should().Be("Available");

        var chooseBossResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{bossNode.Id}/choose",
            content: null);

        var chooseBossBody = await chooseBossResponse.Content.ReadAsStringAsync();

        chooseBossResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: chooseBossBody);

        var finalPayload = await ResolveAndHandleCombatAsync(runId);

        finalPayload.Run.Status.Should().Be("RoomResolved");
        finalPayload.Run.CurrentRoom.State.Should().Be("Completed");

        var allNodes = finalPayload.Run.CurrentRoom.Nodes.ToArray();

        allNodes.Single(node => node.Id == bossNode.Id)
            .State
            .Should()
            .Be("Resolved");
    }
}
