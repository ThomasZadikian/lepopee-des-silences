using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class StartRunEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StartRunEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StartRun_ShouldReturnCreatedRun_WhenRequestIsValid()
    {
        var playerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var response = await _client.PostAsJsonAsync(
            "/api/v2/runs",
            new
            {
                PlayerId = playerId
            });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            because: body); response.Headers.Location.Should().NotBeNull();

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().NotBeEmpty();
        payload.Run.PlayerId.Should().Be(playerId);
        payload.Run.Seed.Should().StartWith("seed-");
        payload.Run.GeneratorVersion.Should().Be("gen-0.3.0");
        payload.Run.MarkovMatrixVersion.Should().Be("markov-room-type-0.1.0");
        payload.Run.Status.Should().Be("Active");
        payload.Run.CurrentDepth.Should().Be(0);

        payload.Run.CurrentRoom.Id.Should().NotBeEmpty();
        payload.Run.CurrentRoom.Depth.Should().Be(0);
        payload.Run.CurrentRoom.RoomType.Should().Be("Threshold");
        payload.Run.CurrentRoom.Theme.Should().Be("Threshold");
        payload.Run.CurrentRoom.State.Should().Be("Active");
        payload.Run.CurrentRoom.CurrentNodeDepth.Should().Be(0);
        payload.Run.CurrentRoom.MaxNodeDepth.Should().BeGreaterThan(0);

        payload.Run.CurrentRoom.BossPreview.Should().NotBeNull();
        payload.Run.CurrentRoom.BossPreview.BossId.Should().Be("threshold-guardian");
        payload.Run.CurrentRoom.BossPreview.Name.Should().Be("Gardien du Seuil");
        payload.Run.CurrentRoom.BossPreview.RoomType.Should().Be("Threshold");
        payload.Run.CurrentRoom.BossPreview.DangerHint.Should().Be("High");

        var allNodes = payload.Run.CurrentRoom.NodeLayers
            .SelectMany(layer => layer.Nodes)
            .ToArray();

        payload.Run.CurrentRoom.TotalNodeCount.Should().BeInRange(6, 10);
        allNodes.Should().HaveCount(payload.Run.CurrentRoom.TotalNodeCount);

        payload.Run.CurrentRoom.NodeLayers.Should().NotBeEmpty();
        payload.Run.CurrentRoom.NodeLayers
            .Select(layer => layer.Depth)
            .Should()
            .OnlyHaveUniqueItems();

        payload.Run.CurrentRoom.NodeLayers
            .Should()
            .OnlyContain(layer => layer.Nodes.Count >= 1 && layer.Nodes.Count <= 4);

        payload.Run.CurrentRoom.AvailableNodes.Should().HaveCountGreaterThanOrEqualTo(1);
        payload.Run.CurrentRoom.AvailableNodes.Should().HaveCountLessThanOrEqualTo(4);
        payload.Run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.State == "Available");
        payload.Run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.NodeDepth == 0);

        allNodes
            .Where(node => node.NodeDepth == 0)
            .Should()
            .OnlyContain(node => node.State == "Available");

        allNodes
            .Where(node => node.NodeDepth > 0)
            .Should()
            .OnlyContain(node => node.State == "Planned");

        allNodes.Should().ContainSingle(node => node.IsRoomBossNode);

        var bossNode = allNodes.Single(node => node.IsRoomBossNode);

        bossNode.State.Should().Be("Planned");
        bossNode.EventTypes.Should().Contain("RoomBoss");
        bossNode.NodeDepth.Should().Be(payload.Run.CurrentRoom.MaxNodeDepth);

        allNodes.Should().OnlyContain(node => node.EventCount >= 1 && node.EventCount <= 4);
        allNodes.Should().OnlyContain(node => node.EventTypes.Count >= 1 && node.EventTypes.Count <= 4);
    }

    [Fact]
    public async Task StartRun_ShouldReturnBadRequest_WhenPlayerIdIsEmpty()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v2/runs",
            new
            {
                PlayerId = Guid.Empty
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Validation failed.");
        body.Should().Contain("Player id is required.");
    }
}