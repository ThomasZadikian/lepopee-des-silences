using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.MoveToNextRoom;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class MoveToNextRoomEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MoveToNextRoomEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MoveToNextRoom_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/rooms/next",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Resource not found.");
        body.Should().Contain($"Run with id '{unknownRunId}' was not found.");
    }

    [Fact]
    public async Task MoveToNextRoom_ShouldReturnBadRequest_WhenCurrentRoomIsNotCompleted()
    {
        var startRunResponse = await StartRunAsync();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/rooms/next",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Domain rule violated.");
    }

    [Fact]
    public async Task MoveToNextRoom_ShouldGenerateNextRoom_WhenCurrentRoomIsCompleted()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var completedRoom = await CompleteCurrentRoomAsync(runId, startRunResponse.Run.CurrentRoom);

        ((string)completedRoom.State).Should().Be("Completed");

        var response = await _client.PostAsync(
            $"/api/v2/runs/{runId}/rooms/next",
            content: null);

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<MoveToNextRoomResponse>();

        payload.Should().NotBeNull();

        payload!.Run.Id.Should().Be(runId);
        payload.Run.Status.Should().Be("Active");
        payload.Run.CurrentDepth.Should().Be(1);
        payload.Run.CurrentRoom.Depth.Should().Be(1);
        payload.Run.CurrentRoom.State.Should().Be("Active");
        payload.Run.CurrentRoom.AvailableNodes.Should().NotBeEmpty();
        payload.Run.CurrentRoom.TotalNodeCount.Should().BeInRange(6, 10);
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
            new { PlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111") });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();

        return payload!;
    }

    private async Task<dynamic> CompleteCurrentRoomAsync(Guid runId, dynamic currentRoom)
    {
        while ((string)currentRoom.State != "Completed")
        {
            var nodeToChoose = currentRoom.AvailableNodes[0];

            var chooseResponse = await _client.PostAsync(
                $"/api/v2/runs/{runId}/nodes/{nodeToChoose.Id}/choose",
                content: null);

            var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

            chooseResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: chooseBody);

            var resolvedPayload = await ResolveCurrentEventAndChooseOptionIfRequiredAsync(runId);

            currentRoom = resolvedPayload.Run.CurrentRoom;

            if ((string)currentRoom.State == "Completed")
            {
                return currentRoom;
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

        return currentRoom;
    }
}