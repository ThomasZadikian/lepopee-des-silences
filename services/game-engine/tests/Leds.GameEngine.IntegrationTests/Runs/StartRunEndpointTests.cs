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

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().NotBeEmpty();
        payload.Run.PlayerId.Should().Be(playerId);
        payload.Run.Seed.Should().StartWith("seed-");
        payload.Run.GeneratorVersion.Should().Be("gen-0.1.0");
        payload.Run.MarkovMatrixVersion.Should().Be("markov-0.1.0");
        payload.Run.Status.Should().Be("Active");
        payload.Run.CurrentDepth.Should().Be(0);
        payload.Run.CurrentRoom.Depth.Should().Be(0);
        payload.Run.CurrentRoom.Theme.Should().Be("Threshold");
        payload.Run.CurrentRoom.Nodes.Should().HaveCount(4);
        payload.Run.CurrentRoom.Nodes.Should().OnlyContain(node => node.State == "Available");
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