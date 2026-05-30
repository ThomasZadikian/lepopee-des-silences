using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class ResolveCurrentEventEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ResolveCurrentEventEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldResolveSelectedNode_AndKeepRunActive()
    {
        var startRunResponse = await StartRunAsync();
        var nodeToChoose = startRunResponse.Run.CurrentRoom.AvailableNodes.First();

        var chooseResponse = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{nodeToChoose.Id}/choose",
            content: null);

        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolveResponse = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/current-event/resolve",
            content: null);

        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await resolveResponse.Content.ReadFromJsonAsync<ResolveCurrentEventResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Status.Should().Be("Active");
        payload.Run.CurrentRoom.State.Should().Be("NodeResolved");

        var resolvedNode = payload.Run.CurrentRoom.NodeLayers.SelectMany(layer => layer.Nodes)
            .Single(node => node.Id == nodeToChoose.Id);

        resolvedNode.State.Should().Be("Resolved");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldReturnBadRequest_WhenNoNodeWasSelected()
    {
        var startRunResponse = await StartRunAsync();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/current-event/resolve",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Domain rule violated.");
        body.Should().Contain("Room must have a selected node before resolving an event.");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/current-event/resolve",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Resource not found.");
        body.Should().Contain($"Run with id '{unknownRunId}' was not found.");
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