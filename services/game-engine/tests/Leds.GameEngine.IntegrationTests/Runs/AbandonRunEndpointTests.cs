using FluentAssertions;
using Leds.GameEngine.Application.Runs.AbandonRun;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

[Collection("GameEngineApi")]
public sealed class AbandonRunEndpointTests : RunIntegrationTestBase
{
    public AbandonRunEndpointTests(GameEngineApiFactory factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task AbandonRun_ShouldReturnOk_FromFreeExplorationSafePoint()
    {
        // A freshly opened spatial room is a safe point: no encounter, combat or reward is active.
        var runId = (await StartRunAsync()).Run.Id;

        // Act
        var response = await Client.PostAsync(
            $"/api/v2/runs/{runId}/abandon",
            content: null);

        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<AbandonRunResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().Be(runId);
        payload.Run.Status.Should().Be("Resolved");
        payload.Run.Outcome.Should().Be("Abandon");
    }

    [Fact]
    public async Task AbandonRun_ShouldReturnOk_WhenRunIsActive()
    {
        // Arrange — freshly started run is Active, not at a safe point
        var startRunResponse = await StartRunAsync();
        var runId = startRunResponse.Run.Id;

        // Act
        var response = await Client.PostAsync(
            $"/api/v2/runs/{runId}/abandon",
            content: null);

        var body = await response.Content.ReadAsStringAsync();

        // Assert — destructive abandonment is available even outside a safe point.
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<AbandonRunResponse>();
        payload.Should().NotBeNull();
        payload!.Run.Status.Should().Be("Resolved");
        payload.Run.Outcome.Should().Be("Abandon");
    }

    [Fact]
    public async Task AbandonRun_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/abandon",
            content: null);

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            because: body);

        body.Should().Contain("Resource not found.");
        body.Should().Contain($"Run with id '{unknownRunId}' was not found.");
    }

    [Fact]
    public async Task AbandonRun_ShouldReturnBadRequest_WhenRunIsAlreadyAbandoned()
    {
        var runId = (await StartRunAsync()).Run.Id;

        var firstResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/abandon",
            content: null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — second abandon attempt
        var secondResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/abandon",
            content: null);

        var body = await secondResponse.Content.ReadAsStringAsync();

        // Assert — the domain rejects a second close operation.
        secondResponse.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Domain rule violated.");
    }
}
