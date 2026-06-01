using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class ProgressRunEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProgressRunEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProgressRun_ShouldUnlockOnlyChildrenOfResolvedNode()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var chosenNode = startRunResponse.Run.CurrentRoom.AvailableNodes.First();

        var chooseResponse = await _client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{chosenNode.Id}/choose",
            content: null);

        var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

        chooseResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: chooseBody);

        await ResolveCurrentEventAndChooseOptionIfRequiredAsync(runId);

        var progressResponse = await _client.PostAsync(
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

        var response = await _client.PostAsync(
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

        var response = await _client.PostAsync(
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

            var chooseResponse = await _client.PostAsync(
                $"/api/v2/runs/{runId}/nodes/{chosenNode.Id}/choose",
                content: null);

            var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

            chooseResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: chooseBody);

            var resolvedPayload = await ResolveCurrentEventAndChooseOptionIfRequiredAsync(runId);

            if (resolvedPayload.Run.CurrentRoom.State == "Completed")
            {
                currentRoom = resolvedPayload.Run.CurrentRoom;
                break;
            }

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
        }

        currentRoom.State.Should().Be("BossReached");
        currentRoom.AvailableNodes.Should().ContainSingle();
        currentRoom.AvailableNodes.Single().IsRoomBossNode.Should().BeTrue();
        currentRoom.AvailableNodes.Single().EventTypes.Should().Contain("RoomBoss");
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