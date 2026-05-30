using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.ChooseNode;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class ChooseNodeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ChooseNodeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ChooseNode_ShouldSelectNode_AndLockOtherNodes()
    {
        var startRunResponse = await StartRunAsync();
        var nodeToChoose = startRunResponse.Run.CurrentRoom.Nodes.First();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{nodeToChoose.Id}/choose",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ChooseNodeResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().Be(startRunResponse.Run.Id);

        var selectedNode = payload.Run.CurrentRoom.Nodes
            .Single(node => node.Id == nodeToChoose.Id);

        selectedNode.State.Should().Be("Selected");

        payload.Run.CurrentRoom.Nodes
            .Where(node => node.Id != nodeToChoose.Id)
            .Should()
            .OnlyContain(node => node.State == "Locked");
    }

    [Fact]
    public async Task ChooseNode_ShouldReturnBadRequest_WhenChoosingSecondNodeAtSameRoomDepth()
    {
        var startRunResponse = await StartRunAsync();

        var firstNode = startRunResponse.Run.CurrentRoom.AvailableNodes.First();
        var secondNode = startRunResponse.Run.CurrentRoom.AvailableNodes.Last();

        var firstResponse = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{firstNode.Id}/choose",
            content: null);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{secondNode.Id}/choose",
            content: null);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await secondResponse.Content.ReadAsStringAsync();

        body.Should().Contain("Domain rule violated.");
        body.Should().Contain("Room is not waiting for a node selection.");
    }

    [Fact]
    public async Task ChooseNode_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/nodes/{nodeId}/choose",
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