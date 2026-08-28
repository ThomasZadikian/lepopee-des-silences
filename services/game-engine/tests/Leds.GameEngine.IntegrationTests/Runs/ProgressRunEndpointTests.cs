using FluentAssertions;
using Leds.GameEngine.Application.Rewards.Dtos;
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

        await ResolveCombatDeterministicallyAsync(runId, selectReward: true);

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
        await ResolveCombatDeterministicallyAsync(runId, selectReward: false);

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
        await ResolveCombatDeterministicallyAsync(runId, selectReward: true);

        var firstProgress = await Client.PostAsync($"/api/v2/runs/{runId}/progress", null);
        var firstProgressBody = await firstProgress.Content.ReadAsStringAsync();
        firstProgress.StatusCode.Should().Be(HttpStatusCode.OK, because: firstProgressBody);

        var progressed = await firstProgress.Content.ReadFromJsonAsync<ProgressRunResponse>();
        progressed.Should().NotBeNull();

        progressed!.Run.CurrentRoom.Nodes.Should().Contain(node =>
            node.Id != firstNode.Id && node.State == "Available");
    }

    private async Task ResolveCombatDeterministicallyAsync(Guid runId, bool selectReward)
    {
        var resolveResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/current-event/resolve", null);
        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: resolveBody);

        var resolvePayload = await resolveResponse.Content
            .ReadFromJsonAsync<ResolveCurrentEventResponse>();
        resolvePayload.Should().NotBeNull(because: resolveBody);
        resolvePayload!.Run.ActiveCombatId.Should().NotBeNull(
            because: "these progression fixtures deliberately select a combat node");

        using var killRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/dev/v2/runs/{runId}/combats/current/kill-enemies")
        {
            Content = JsonContent.Create(new { })
        };
        killRequest.Headers.Add(
            "X-Leds-DevTools-Token",
            GameEngineApiFactory.DevToolsToken);

        var killResponse = await Client.SendAsync(killRequest);
        var killBody = await killResponse.Content.ReadAsStringAsync();
        killResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: killBody);

        if (!selectReward)
        {
            return;
        }

        var pendingResponse = await Client.GetAsync(
            $"/api/v2/runs/{runId}/rewards/pending");
        if (pendingResponse.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var rewardOffer = await pendingResponse.Content.ReadFromJsonAsync<RewardOfferDto>();
        if (rewardOffer?.SelectedChoiceId is not null || rewardOffer?.Choices.Count is not > 0)
        {
            return;
        }

        var affordableChoice = rewardOffer.Choices.FirstOrDefault(choice =>
            choice.PalaceShardCost == 0 && choice.HimLitShardCost == 0);
        affordableChoice.Should().NotBeNull(
            because: "every generated offer must expose a free reward or decline choice");

        var selectResponse = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/rewards/select",
            new { ChoiceId = affordableChoice!.Id });
        var selectBody = await selectResponse.Content.ReadAsStringAsync();
        selectResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: selectBody);
    }
}
