using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.SubmitCombatAction;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class ProgressRunEndpointTests : RunIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public ProgressRunEndpointTests(WebApplicationFactory<Program> factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task ProgressRun_ShouldUnlockOnlyChildrenOfResolvedNode()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var chosenNode = startRunResponse.Run.CurrentRoom.AvailableNodes.First();

        var chooseResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{chosenNode.Id}/choose",
            content: null);

        var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

        chooseResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: chooseBody);

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
        payload.Run.CurrentRoom.CurrentNodeDepth.Should().Be(1);
        payload.Run.CurrentRoom.State.Should().BeOneOf("Active", "BossReached");

        payload.Run.CurrentRoom.AvailableNodes.Should().NotBeEmpty();

        payload.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.ParentNodeIds.Contains(chosenNode.Id));

        payload.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.State == "Available");

        payload.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.Row == payload.Run.CurrentRoom.CurrentNodeDepth);
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
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;

        var chosenNode = startRunResponse.Run.CurrentRoom.AvailableNodes
            .FirstOrDefault(n => n.Type == "Combat");

        if (chosenNode is null)
        {
            return; // Skip when no node has Combat as primary event
        }

        var chooseResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{chosenNode.Id}/choose",
            content: null);

        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

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
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;

        var chosenNode = startRunResponse.Run.CurrentRoom.AvailableNodes
            .FirstOrDefault(n => n.Type == "Combat");

        if (chosenNode is null)
        {
            return; // Skip when no node has Combat as primary event
        }

        var chooseResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{chosenNode.Id}/choose",
            content: null);

        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Full resolve + complete combat to create a reward offer
        var resolveResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/current-event/resolve", null);

        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();

        resolveResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: resolveBody);

        var resolvePayload = await resolveResponse.Content
            .ReadFromJsonAsync<ResolveCurrentEventResponse>();

        resolvePayload.Should().NotBeNull();

        if (resolvePayload!.Run.ActiveCombatId is not null)
        {
            await CompleteActiveCombatAsyncWithoutSelectingRewardAsync(
                runId, resolvePayload.Run.ActiveCombatId.Value);
        }
        else
        {
            return; // Skip when no combat was created
        }

        // Try to progress while reward is pending
        var progressResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/progress",
            content: null);

        progressResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var progressBody = await progressResponse.Content.ReadAsStringAsync();

        progressBody.Should().Contain("Domain rule violated.");
        progressBody.Should().Contain("Cannot progress while a pending reward offer requires selection.");
    }

    private async Task CompleteActiveCombatAsyncWithoutSelectingRewardAsync(
        Guid runId, Guid combatId)
    {
        var combatResponse = await Client.GetAsync(
            $"/api/v2/runs/{runId}/combats/{combatId}");

        combatResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var combat = await combatResponse.Content
            .ReadFromJsonAsync<CombatInstanceDto>();

        combat.Should().NotBeNull();

        var playerId = combat!.Combatants.Single(c => c.Side == "Player").Id;
        var enemyId = combat.Combatants.Single(c => c.Side == "Enemy").Id;

        while (true)
        {
            var actionResponse = await Client.PostAsJsonAsync(
                $"/api/v2/runs/{runId}/combats/{combatId}/actions",
                new { ActorId = playerId, TargetId = enemyId, ActionType = "BasicAttack" });

            var actionBody = await actionResponse.Content.ReadAsStringAsync();

            actionResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: actionBody);

            var actionResult = await actionResponse.Content
                .ReadFromJsonAsync<SubmitCombatActionResponse>();

            actionResult.Should().NotBeNull();

            if (actionResult!.Result.CombatState == "Completed")
            {
                break;
            }
        }
    }

    [Fact]
    public async Task ProgressRun_ShouldEventuallyReachRoomBoss()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var currentRoom = startRunResponse.Run.CurrentRoom;

        while (currentRoom.State != "BossReached")
        {
            var chosenNode = currentRoom.AvailableNodes.First();

            var chooseResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/nodes/{chosenNode.Id}/choose",
                content: null);

            var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

            chooseResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: chooseBody);

            var resolvedPayload = await ResolveAndHandleCombatAsync(runId);

            if (resolvedPayload.Run.CurrentRoom.State == "Completed")
            {
                currentRoom = resolvedPayload.Run.CurrentRoom;
                break;
            }

            var progressResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/progress",
                content: null);

            var progressBody = await progressResponse.Content.ReadAsStringAsync();

            progressResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: progressBody);

            var progressPayload = await progressResponse.Content
                .ReadFromJsonAsync<ProgressRunResponse>();

            progressPayload.Should().NotBeNull();

            currentRoom = progressPayload!.Run.CurrentRoom;
        }

        currentRoom.State.Should().Be("BossReached");
        currentRoom.AvailableNodes.Should().ContainSingle();
        currentRoom.AvailableNodes.Single().IsBoss.Should().BeTrue();
        currentRoom.AvailableNodes.Single().Type.Should().Be("RoomBoss");
    }
}