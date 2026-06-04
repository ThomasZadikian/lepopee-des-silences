using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class ProgressRunEndpointTests : RunIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public ProgressRunEndpointTests(WebApplicationFactory<Program> factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task ProgressRun_ShouldUnlockOnlyChildrenOfResolvedNode()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var chosenNode = startRunResponse.Run.CurrentRoom.AvailableNodes.First();

        var chooseResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{chosenNode.Id}/choose",
            content: null);

        var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

        chooseResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: chooseBody);

        await ResolveAndHandleCombatAsync(runId);

        var progressResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/progress",
            content: null);

        var progressBody = await progressResponse.Content.ReadAsStringAsync();

        progressResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: progressBody);

        var payload = await progressResponse.Content.ReadFromJsonAsync<ProgressRunResponse>();

        payload.Should().NotBeNull();

        payload!.Run.Status.Should().Be("Active");
        payload.Run.CurrentRoom.CurrentNodeDepth.Should().Be(1);
        payload.Run.CurrentRoom.State.Should().BeOneOf("Active", "BossReached");

        payload.Run.CurrentRoom.AvailableNodes.Should().NotBeEmpty();

        payload.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.ParentNodeIds.Contains(chosenNode.Id));

        payload.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.State == "Available");

        payload.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.NodeDepth == payload.Run.CurrentRoom.CurrentNodeDepth);
    }

    [Fact]
    public async Task ProgressRun_ShouldReturnBadRequest_WhenCurrentEventIsNotResolved()
    {
        var startRunResponse = await StartRunAsync();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/progress",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Domain rule violated.");
        body.Should().Contain("Current node event must be resolved before progressing.");
    }

    [Fact]
    public async Task ProgressRun_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/progress",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Resource not found.");
        body.Should().Contain($"Run with id '{unknownRunId}' was not found.");
    }

    [Fact]
    public async Task ProgressRun_ShouldEventuallyReachRoomBoss()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var currentRoom = startRunResponse.Run.CurrentRoom;

        while (currentRoom.State != "BossReached")
        {
            var chosenNode = currentRoom.AvailableNodes.First();

            var chooseResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/nodes/{chosenNode.Id}/choose",
                content: null);

            var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

            chooseResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: chooseBody);

            var resolvedPayload = await ResolveAndHandleCombatAsync(runId);

            if (resolvedPayload.Run.CurrentRoom.State == "Completed")
            {
                currentRoom = resolvedPayload.Run.CurrentRoom;
                break;
            }

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
        }

        currentRoom.State.Should().Be("BossReached");
        currentRoom.AvailableNodes.Should().ContainSingle();
        currentRoom.AvailableNodes.Single().IsRoomBossNode.Should().BeTrue();
        currentRoom.AvailableNodes.Single().EventTypes.Should().Contain("RoomBoss");
    }
}