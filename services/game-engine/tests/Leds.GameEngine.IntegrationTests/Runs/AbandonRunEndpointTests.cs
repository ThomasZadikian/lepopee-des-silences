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

    /// <summary>
    /// Drives the run to RoomResolved by choosing and resolving all nodes up to and including
    /// the boss, then collecting the boss reward. Returns the run Id.
    /// </summary>
    private async Task<Guid> StartRunAtSafePointAsync()
    {
        var startRunResponse = await StartRunAsync();
        var runId = startRunResponse.Run.Id;
        var currentRoom = startRunResponse.Run.CurrentRoom;

        // Navigate through all non-boss layers
        while (currentRoom.CurrentNodeDepth < currentRoom.MaxNodeDepth)
        {
            var nodeToChoose = currentRoom.AvailableNodes.First();

            var chooseResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/nodes/{nodeToChoose.Id}/choose",
                content: null);
            chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var resolved = await ResolveAndHandleCombatAsync(runId);

            var progressResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/progress",
                content: null);
            progressResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var progressBody = await progressResponse.Content
                .ReadFromJsonAsync<Leds.GameEngine.Application.Runs.ProgressRun.ProgressRunResponse>();
            currentRoom = progressBody!.Run.CurrentRoom;
        }

        // Choose and resolve the boss node
        var bossNode = currentRoom.AvailableNodes.Single();
        bossNode.IsBoss.Should().BeTrue();

        var chooseBoss = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{bossNode.Id}/choose",
            content: null);
        chooseBoss.StatusCode.Should().Be(HttpStatusCode.OK);

        var bossResolved = await ResolveAndHandleCombatAsync(runId);
        bossResolved.Run.Status.Should().Be("RoomResolved");

        return runId;
    }

    [Fact]
    public async Task AbandonRun_ShouldReturnOk_WhenRunIsAtSafePoint()
    {
        // Arrange — drive run to RoomResolved (safe point)
        var runId = await StartRunAtSafePointAsync();

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
        // Arrange — drive to safe point, then abandon successfully
        var runId = await StartRunAtSafePointAsync();

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
