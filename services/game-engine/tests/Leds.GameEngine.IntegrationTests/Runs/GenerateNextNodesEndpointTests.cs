using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.GenerateNextNodes;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class GenerateNextNodesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GenerateNextNodesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GenerateNextNodes_ShouldAddNextNodeLayer_WhenCurrentEventIsResolved()
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

        var nextNodesResponse = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/next",
            content: null);

        nextNodesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await nextNodesResponse.Content.ReadFromJsonAsync<GenerateNextNodesResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Status.Should().Be("Active");
        payload.Run.CurrentRoom.CurrentNodeDepth.Should().Be(1);
        payload.Run.CurrentRoom.AvailableNodes.Should().NotBeEmpty();
        payload.Run.CurrentRoom.AvailableNodes.Count.Should().BeInRange(1, 4);
        payload.Run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.NodeDepth == 1);
        payload.Run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.ParentNodeId == nodeToChoose.Id);
        payload.Run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.State == "Available");
    }

    [Fact]
    public async Task GenerateNextNodes_ShouldReturnBadRequest_WhenCurrentEventIsNotResolved()
    {
        var startRunResponse = await StartRunAsync();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/next",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Domain rule violated.");
        body.Should().Contain("Current node event must be resolved before adding next nodes.");
    }

    [Fact]
    public async Task GenerateNextNodes_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/nodes/next",
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