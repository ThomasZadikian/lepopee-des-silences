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
    public async Task ChooseNode_ShouldSelectNode_LockSiblings_AndMarkUnreachableBranches()
    {
        var startRunResponse = await StartRunAsync();
        var nodeToChoose = startRunResponse.Run.CurrentRoom.AvailableNodes.First();

        var response = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{nodeToChoose.Id}/choose",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ChooseNodeResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().Be(startRunResponse.Run.Id);
        payload.Run.CurrentRoom.State.Should().Be("NodeSelected");
        payload.Run.CurrentRoom.CurrentNodeDepth.Should().Be(0);

        var allNodes = payload.Run.CurrentRoom.NodeLayers
            .SelectMany(layer => layer.Nodes)
            .ToArray();

        payload.Run.CurrentRoom.TotalNodeCount.Should().BeInRange(6, 10);
        allNodes.Should().HaveCount(payload.Run.CurrentRoom.TotalNodeCount);

        var selectedNode = allNodes.Single(node => node.Id == nodeToChoose.Id);

        selectedNode.State.Should().Be("Selected");
        selectedNode.NodeDepth.Should().Be(0);

        allNodes
            .Where(node => node.NodeDepth == 0 && node.Id != nodeToChoose.Id)
            .Should()
            .OnlyContain(node => node.State == "Locked");

        allNodes
            .Where(node => node.NodeDepth > 0)
            .Should()
            .OnlyContain(node => node.State == "Planned" || node.State == "Unreachable");

        allNodes
            .Where(node => node.NodeDepth > 0 && IsReachableFrom(nodeToChoose.Id, node, allNodes))
            .Should()
            .OnlyContain(node => node.State == "Planned");

        allNodes
            .Where(node => node.NodeDepth > 0 && !IsReachableFrom(nodeToChoose.Id, node, allNodes))
            .Should()
            .OnlyContain(node => node.State == "Unreachable");

        payload.Run.CurrentRoom.AvailableNodes.Should().BeEmpty();

        allNodes.Should().ContainSingle(node => node.IsRoomBossNode);
        allNodes.Should().OnlyContain(node => node.EventCount >= 1 && node.EventCount <= 4);
        allNodes.Should().OnlyContain(node => node.EventTypes.Count >= 1 && node.EventTypes.Count <= 4);

        payload.Run.CurrentRoom.BossPreview.Should().NotBeNull();
        payload.Run.CurrentRoom.BossPreview.Name.Should().NotBeNullOrWhiteSpace();
    }

    private static bool IsReachableFrom(
        Guid ancestorNodeId,
        Leds.GameEngine.Application.Runs.Dtos.NodeDto node,
        IReadOnlyCollection<Leds.GameEngine.Application.Runs.Dtos.NodeDto> allNodes)
    {
        if (node.ParentNodeIds.Contains(ancestorNodeId))
        {
            return true;
        }

        foreach (var parentNodeId in node.ParentNodeIds)
        {
            var parent = allNodes.SingleOrDefault(candidate => candidate.Id == parentNodeId);

            if (parent is not null && IsReachableFrom(ancestorNodeId, parent, allNodes))
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public async Task ChooseNode_ShouldReturnBadRequest_WhenChoosingSecondNodeAtSameRoomDepth()
    {
        var startRunResponse = await StartRunAsync();

        var availableNodes = startRunResponse.Run.CurrentRoom.AvailableNodes.ToArray();

        availableNodes.Should().HaveCountGreaterThanOrEqualTo(2);

        var firstNode = availableNodes.First();
        var secondNode = availableNodes.Last();

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
    public async Task ChooseNode_ShouldReturnBadRequest_WhenChoosingPlannedNode()
    {
        var startRunResponse = await StartRunAsync();

        var plannedNode = startRunResponse.Run.CurrentRoom.NodeLayers
            .SelectMany(layer => layer.Nodes)
            .First(node => node.State == "Planned");

        var response = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{plannedNode.Id}/choose",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Domain rule violated.");
        body.Should().Contain("Only a node from the current room depth can be selected.");
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

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();

        return payload!;
    }
}