using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class RoomBossProgressionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RoomBossProgressionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
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

            var chooseResponse = await _client.PostAsync(
                $"/api/v2/runs/{runId}/nodes/{nodeToChoose.Id}/choose",
                content: null);

            var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

            chooseResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: chooseBody);

            var resolvedPayload = await ResolveCurrentEventAndChooseOptionIfRequiredAsync(runId);

            resolvedPayload.Run.Status.Should().Be("Active");
            resolvedPayload.Run.CurrentRoom.State.Should().Be("NodeResolved");

            var progressResponse = await _client.PostAsync(
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
                node.NodeDepth == currentRoom.CurrentNodeDepth);
        }

        currentRoom.State.Should().Be("BossReached");
        currentRoom.CurrentNodeDepth.Should().Be(currentRoom.MaxNodeDepth);
        currentRoom.AvailableNodes.Should().ContainSingle();

        var bossNode = currentRoom.AvailableNodes.Single();

        bossNode.EventTypes.Should().Contain("RoomBoss");
        bossNode.IsRoomBossNode.Should().BeTrue();
        bossNode.State.Should().Be("Available");

        var chooseBossResponse = await _client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{bossNode.Id}/choose",
            content: null);

        var chooseBossBody = await chooseBossResponse.Content.ReadAsStringAsync();

        chooseBossResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: chooseBossBody);

        var finalPayload = await ResolveCurrentEventAndChooseOptionIfRequiredAsync(runId);

        finalPayload.Run.Status.Should().Be("RoomResolved");
        finalPayload.Run.CurrentRoom.State.Should().Be("Completed");

        var allNodes = finalPayload.Run.CurrentRoom.NodeLayers
            .SelectMany(layer => layer.Nodes)
            .ToArray();

        allNodes.Single(node => node.Id == bossNode.Id)
            .State
            .Should()
            .Be("Resolved");
    }

    private async Task<ResolveCurrentEventResponse> ResolveCurrentEventAndChooseOptionIfRequiredAsync(
        Guid runId)
    {
        var resolveResponse = await _client.PostAsync(
            $"/api/v2/runs/{runId}/current-event/resolve",
            content: null);

        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();

        resolveResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: resolveBody);

        var resolvePayload = await resolveResponse.Content
            .ReadFromJsonAsync<ResolveCurrentEventResponse>();

        resolvePayload.Should().NotBeNull();

        if (!resolvePayload!.Outcome.RequiresPlayerChoice)
        {
            return resolvePayload;
        }

        resolvePayload.Outcome.Choices.Should()
            .NotBeEmpty("an event requiring a player choice must expose at least one available choice.");

        var firstChoice = resolvePayload.Outcome.Choices.First();

        var choiceResponse = await _client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/current-event/choice",
            new { firstChoice.ChoiceId });

        var choiceBody = await choiceResponse.Content.ReadAsStringAsync();

        choiceResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: choiceBody);

        return resolvePayload;
    }

    private async Task<StartRunResponse> StartRunAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v2/runs",
            new
            {
                PlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111")
            });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();

        return payload!;
    }
}