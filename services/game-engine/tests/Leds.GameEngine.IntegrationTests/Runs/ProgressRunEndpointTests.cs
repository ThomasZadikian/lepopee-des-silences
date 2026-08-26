using FluentAssertions;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

[Collection("GameEngineApi")]
public sealed class ProgressRunEndpointTests : RunIntegrationTestBase
{
    public ProgressRunEndpointTests(GameEngineApiFactory factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task ProgressRun_ShouldReturnToFreeExploration_AfterResolvedNode()
    {
        var (run, chosenNode) = await StartRunWithCombatNodeAsync();

        var runId = run.Id;
        await MovePartyToNodeAsync(runId, chosenNode);

        await ResolveAndHandleCombatAsync(runId);

        var progressResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/progress",
            content: null);

        var progressBody = await progressResponse.Content.ReadAsStringAsync();

        progressResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: progressBody);

        var payload = await progressResponse.Content.ReadFromJsonAsync<ProgressRunResponse>();

        payload.Should().NotBeNull();

        payload!.Run.Status.Should().Be("Active");
        payload.Run.CurrentRoom.State.Should().Be("Active");
        payload.Run.CurrentRoom.Nodes.Single(node => node.Id == chosenNode.Id)
            .State.Should().Be("Resolved");
    }

    [Fact]
    public async Task ProgressRun_ShouldReturnBadRequest_WhenCurrentEventIsNotResolved()
    {
        var startRunResponse = await StartRunAsync();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/progress",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Domain rule violated.");
        body.Should().Contain("Current node event must be resolved before progressing.");
    }

    [Fact]
    public async Task ProgressRun_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/progress",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Resource not found.");
        body.Should().Contain($"Run with id '{unknownRunId}' was not found.");
    }

    [Fact]
    public async Task ProgressRun_ShouldReturnBadRequest_WhenCombatIsActive()
    {
        var (run, chosenNode) = await StartRunWithCombatNodeAsync();

        var runId = run.Id;
        await MovePartyToNodeAsync(runId, chosenNode);

        // Resolve event to create a combat (don't complete it)
        var resolveResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/current-event/resolve", null);

        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();

        resolveResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: resolveBody);

        // Try to progress while combat is active
        var progressResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/progress",
            content: null);

        progressResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var progressBody = await progressResponse.Content.ReadAsStringAsync();

        progressBody.Should().Contain("Domain rule violated.");
        progressBody.Should().Contain("Cannot progress while a combat is active.");
    }

    [Fact]
    public async Task ProgressRun_ShouldReturnBadRequest_WhenRewardIsPending()
    {
        var (run, chosenNode) = await StartRunWithCombatNodeAsync();
        var runId = run.Id;
        await MovePartyToNodeAsync(runId, chosenNode);

        // Resolve and complete a combat without claiming its guaranteed reward offer.
        var resolveResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/current-event/resolve", null);

        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();

        resolveResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: resolveBody);

        var resolvePayload = await resolveResponse.Content
            .ReadFromJsonAsync<ResolveCurrentEventResponse>();

        resolvePayload.Should().NotBeNull();

        resolvePayload!.Run.ActiveCombatId.Should().NotBeNull();
        await CompleteActiveCombatAsync(
            runId,
            resolvePayload.Run.ActiveCombatId!.Value,
            selectReward: false);

        // Try to progress while reward is pending
        var progressResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/progress",
            content: null);

        progressResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var progressBody = await progressResponse.Content.ReadAsStringAsync();

        progressBody.Should().Contain("Domain rule violated.");
        progressBody.Should().Contain("Cannot progress while a pending reward offer requires selection.");
    }

    [Fact]
    public async Task ProgressRun_ShouldKeepOtherSpatialObjectivesAvailable()
    {
        var (run, firstNode) = await StartRunWithCombatNodeAsync();

        var runId = run.Id;
        await MovePartyToNodeAsync(runId, firstNode);
        await ResolveAndHandleCombatAsync(runId);

        var firstProgress = await Client.PostAsync($"/api/v2/runs/{runId}/progress", null);
        var firstProgressBody = await firstProgress.Content.ReadAsStringAsync();
        firstProgress.StatusCode.Should().Be(HttpStatusCode.OK, because: firstProgressBody);

        var progressed = await firstProgress.Content.ReadFromJsonAsync<ProgressRunResponse>();
        progressed.Should().NotBeNull();

        progressed!.Run.CurrentRoom.Nodes.Should().Contain(node =>
            node.Id != firstNode.Id && node.State == "Available");
    }
}
