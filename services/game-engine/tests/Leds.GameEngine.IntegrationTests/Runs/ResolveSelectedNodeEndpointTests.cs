using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.ResolveSelectedNode;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class ResolveSelectedNodeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ResolveSelectedNodeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ResolveSelectedNode_ShouldResolveSelectedNode_AndSetRunStatusToRoomResolved()
    {
        var startRunResponse = await StartRunAsync();
        var nodeToChoose = startRunResponse.Run.CurrentRoom.Nodes.First();

        var chooseResponse = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{nodeToChoose.Id}/choose",
            content: null);

        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolveResponse = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/selected-node/resolve",
            content: null);

        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await resolveResponse.Content.ReadFromJsonAsync<ResolveSelectedNodeResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Status.Should().Be("RoomResolved");

        var resolvedNode = payload.Run.CurrentRoom.Nodes
            .Single(node => node.Id == nodeToChoose.Id);

        resolvedNode.State.Should().Be("Resolved");
    }

    [Fact]
    public async Task ResolveSelectedNode_ShouldReturnBadRequest_WhenNoNodeWasSelected()
    {
        var startRunResponse = await StartRunAsync();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/selected-node/resolve",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Domain rule violated.");
        body.Should().Contain("No node has been selected for the current room.");
    }

    [Fact]
    public async Task ResolveSelectedNode_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/selected-node/resolve",
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

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();

        return payload!;
    }
}