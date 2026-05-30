using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class GetRunByIdEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GetRunByIdEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRunById_ShouldReturnRun_WhenRunExists()
    {
        var startRunResponse = await StartRunAsync();

        var response = await _client.GetAsync($"/api/v2/runs/{startRunResponse.Run.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GetRunByIdResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().Be(startRunResponse.Run.Id);
        payload.Run.PlayerId.Should().Be(startRunResponse.Run.PlayerId);
        payload.Run.Status.Should().Be("Active");
        payload.Run.CurrentRoom.NodeLayers.SelectMany(layer => layer.Nodes).Should().HaveCount(4);
    }

    [Fact]
    public async Task GetRunById_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v2/runs/{unknownRunId}");

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