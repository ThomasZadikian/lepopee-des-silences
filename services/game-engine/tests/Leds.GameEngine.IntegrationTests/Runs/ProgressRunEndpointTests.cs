using FluentAssertions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.TacticalCombat;
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
            $"/api/v2/runs/{runId}/tactical-combat");

        combatResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var combat = await combatResponse.Content
            .ReadFromJsonAsync<TacticalCombatRuntimeDto>();

        combat.Should().NotBeNull();

        while (combat!.Status != "Completed")
        {
            var isPlayerTurn = combat.Allies.Any(a => a.Combatant.Id == combat.ActiveCombatantId);

            if (isPlayerTurn)
            {
                var enemy = combat.Enemies.FirstOrDefault(e => e.Combatant.CurrentVitality > 0);

                enemy.Should().NotBeNull(
                    because: "an in-progress combat should have at least one living enemy target");

                var skillResponse = await Client.PostAsJsonAsync(
                    $"/api/v2/runs/{runId}/tactical-combat/skill",
                    new { SkillKey = "skill.basic.strike", TargetX = enemy!.X, TargetY = enemy.Y });

                var skillBody = await skillResponse.Content.ReadAsStringAsync();

                skillResponse.StatusCode.Should().Be(
                    HttpStatusCode.OK,
                    because: skillBody);

                var skillResult = await skillResponse.Content
                    .ReadFromJsonAsync<TacticalCombatResponse>();

                skillResult.Should().NotBeNull();

                if (skillResult!.Combat.Status == "Completed")
                {
                    break;
                }

                combat = skillResult.Combat;
            }
            else
            {
                var endTurnResponse = await Client.PostAsync(
                    $"/api/v2/runs/{runId}/tactical-combat/end-turn", null);

                var endTurnBody = await endTurnResponse.Content.ReadAsStringAsync();

                endTurnResponse.StatusCode.Should().Be(
                    HttpStatusCode.OK,
                    because: endTurnBody);

                var endTurnResult = await endTurnResponse.Content
                    .ReadFromJsonAsync<TacticalCombatResponse>();

                endTurnResult.Should().NotBeNull();

                if (endTurnResult!.Combat.Status == "Completed")
                {
                    break;
                }

                combat = endTurnResult.Combat;
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
