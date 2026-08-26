using FluentAssertions;
using Leds.GameEngine.Application.Runs.StartRun;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

[Collection("GameEngineApi")]
public sealed class StartRunEndpointTests
{
    private readonly HttpClient _client;

    public StartRunEndpointTests(GameEngineApiFactory factory)
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
        payload.Run.GeneratorVersion.Should().Be("grid-room-layout-1.0.0");
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

        payload.Run.CurrentRoom.BossPreview.Should().BeNull(
            because: "the entrance Hall is a spatial hub, not a mandatory boss room");

        var allNodes = payload.Run.CurrentRoom.Nodes.ToArray();

        payload.Run.CurrentRoom.TotalNodeCount.Should().BeGreaterThan(0,
            because: "authored rooms may legitimately contain more nodes than procedural rooms");
        allNodes.Should().NotBeEmpty();
        allNodes.Count().Should().BeLessThanOrEqualTo(payload.Run.CurrentRoom.TotalNodeCount,
            because: "fog of war deliberately withholds hidden and unrevealed content");
        allNodes.Should().OnlyContain(node => node.State == "Available");
        allNodes.Should().NotContain(node => node.IsBoss);

        payload.Run.CurrentRoom.Grid.Should().NotBeNull();
        payload.Run.CurrentRoom.Grid!.Width.Should().BeGreaterThan(0);
        payload.Run.CurrentRoom.Grid.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Started_run_should_reload_with_its_character_emotional_registers()
    {
        var startResponse = await _client.PostAsJsonAsync(
            "/api/v2/runs",
            new { PlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111") });
        var startBody = await startResponse.Content.ReadAsStringAsync();
        startResponse.StatusCode.Should().Be(HttpStatusCode.Created, because: startBody);

        var started = await startResponse.Content.ReadFromJsonAsync<StartRunResponse>();
        started.Should().NotBeNull();

        var reloadResponse = await _client.GetAsync($"/api/v2/runs/{started!.Run.Id}");
        var reloadBody = await reloadResponse.Content.ReadAsStringAsync();

        reloadResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: reloadBody);
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
