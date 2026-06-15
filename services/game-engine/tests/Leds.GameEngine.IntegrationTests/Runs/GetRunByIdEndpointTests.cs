using FluentAssertions;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

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
        var allNodes = payload!.Run.CurrentRoom.Nodes.ToArray();

        payload.Run.CurrentRoom.TotalNodeCount.Should().BeInRange(6, 30);
        allNodes.Should().HaveCount(payload.Run.CurrentRoom.TotalNodeCount);

        payload.Run.CurrentRoom.AvailableNodes.Should().HaveCountGreaterThanOrEqualTo(1);
        payload.Run.CurrentRoom.AvailableNodes.Should().HaveCountLessThanOrEqualTo(4);
        payload.Run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.State == "Available");
        payload.Run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.Row == 0);

        allNodes.Where(node => node.Row > 0)
            .Should()
            .OnlyContain(node => node.State == "Planned");

        allNodes.Should().ContainSingle(node => node.IsBoss);

        payload.Run.CurrentRoom.BossPreview.Should().NotBeNull();
        payload.Run.CurrentRoom.BossPreview.Name.Should().NotBeNullOrWhiteSpace();
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

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();

        return payload!;
    }
}