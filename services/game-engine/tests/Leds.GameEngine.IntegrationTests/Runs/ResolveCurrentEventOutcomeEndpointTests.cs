using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class ResolveCurrentEventOutcomeEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ResolveCurrentEventOutcomeEndpointTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldReturnOutcome_WhenNodeIsSelected()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var nodeId = startRunResponse.Run.CurrentRoom.AvailableNodes.First().Id;

        var chooseResponse = await _client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{nodeId}/choose",
            content: null);

        var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

        chooseResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: chooseBody);

        var resolveResponse = await _client.PostAsync(
            $"/api/v2/runs/{runId}/current-event/resolve",
            content: null);

        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();

        resolveResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: resolveBody);

        var payload = await resolveResponse.Content
            .ReadFromJsonAsync<ResolveCurrentEventResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().Be(runId);

        payload.Outcome.NodeId.Should().Be(nodeId);
        payload.Outcome.EventTypes.Should().NotBeEmpty();
        payload.Outcome.PrimaryEventType.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.ResolutionKind.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.Title.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.Description.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.RiskLevel.Should().BeInRange(0, 100);
        payload.Outcome.RewardProfile.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.NarrativeFragments.Should().NotBeNull();
    }

    private async Task<StartRunResponse> StartRunAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v2/runs",
            new { PlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111") });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();

        return payload!;
    }
}