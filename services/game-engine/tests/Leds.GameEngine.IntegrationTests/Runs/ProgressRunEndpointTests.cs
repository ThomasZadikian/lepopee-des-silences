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
    public async Task ProgressRun_ShouldReturnToFreeExploration_AfterResolvedNode()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var chosenNode = FirstContactCombatNode(startRunResponse.Run.CurrentRoom);
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
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;

        var chosenNode = FirstContactCombatNode(startRunResponse.Run.CurrentRoom);
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
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;

        var chosenNode = FirstContactCombatNode(startRunResponse.Run.CurrentRoom);
        await MovePartyToNodeAsync(runId, chosenNode);

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
            throw new InvalidOperationException("A contact combat node must create a combat.");
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
    public async Task ProgressRun_ShouldAllowResolvingAnotherSpatialEncounter()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var firstNode = FirstContactCombatNode(startRunResponse.Run.CurrentRoom);
        await MovePartyToNodeAsync(runId, firstNode);
        await ResolveAndHandleCombatAsync(runId);

        var firstProgress = await Client.PostAsync($"/api/v2/runs/{runId}/progress", null);
        var firstProgressBody = await firstProgress.Content.ReadAsStringAsync();
        firstProgress.StatusCode.Should().Be(HttpStatusCode.OK, because: firstProgressBody);

        var progressed = await firstProgress.Content.ReadFromJsonAsync<ProgressRunResponse>();
        progressed.Should().NotBeNull();

        var grid = progressed!.Run.CurrentRoom.Grid!;
        var secondNode = progressed.Run.CurrentRoom.Nodes
            .Where(node => node.Id != firstNode.Id
                && node.State == "Available"
                && node.ContactBehavior == "TriggerOnEnter"
                && node.Type is "Combat" or "Elite" or "Rare" or "RoomBoss" or "FinalBoss")
            .OrderBy(node => Math.Abs(node.Lane - grid.PartyX) + Math.Abs(node.Row - grid.PartyY))
            .First();

        await MovePartyToNodeAsync(runId, secondNode);
        var secondResolved = await ResolveAndHandleCombatAsync(runId);

        secondResolved.Run.CurrentRoom.State.Should().Be("NodeResolved");
        secondResolved.Run.CurrentRoom.Nodes.Single(node => node.Id == secondNode.Id)
            .State.Should().Be("Resolved");
    }
}
