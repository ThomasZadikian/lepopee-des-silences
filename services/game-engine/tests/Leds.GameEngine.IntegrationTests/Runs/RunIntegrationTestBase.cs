using FluentAssertions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.TacticalCombat;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Rewards.Dtos;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Application.Runs.MoveParty;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Application.Runs.StartRun;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

public abstract class RunIntegrationTestBase
{
    protected readonly HttpClient Client;

    protected RunIntegrationTestBase(HttpClient client)
    {
        Client = client;
    }

    protected async Task<ResolveCurrentEventResponse> ResolveAndHandleCombatAsync(Guid runId)
    {
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
            await CompleteActiveCombatAsync(runId, resolvePayload.Run.ActiveCombatId.Value);
        }

        await ResolveEventChoiceIfRequiredAsync(runId, resolvePayload.Outcome);
        await SelectPendingRewardIfAnyAsync(runId);

        var getResponse = await Client.GetAsync($"/api/v2/runs/{runId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getPayload = await getResponse.Content
            .ReadFromJsonAsync<GetRunByIdResponse>();

        getPayload.Should().NotBeNull();

        return new ResolveCurrentEventResponse(
            getPayload!.Run,
            new ResolvedNodeEventOutcomeDto(
                Guid.Empty, [], string.Empty, string.Empty,
                0, string.Empty, string.Empty, string.Empty,
                false, [], []));
    }

    protected static MapNodeDto FirstContactCombatNode(RoomDto room) =>
        room.Nodes.First(node =>
            node.State == "Available"
            && node.ContactBehavior == "TriggerOnEnter"
            && node.Type is "Combat" or "Elite" or "Rare" or "RoomBoss" or "FinalBoss");

    protected async Task<MovePartyResponse> MovePartyToNodeAsync(Guid runId, MapNodeDto node)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/party/move",
            new { TargetX = node.Lane, TargetY = node.Row });
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<MovePartyResponse>();
        payload.Should().NotBeNull(because: body);
        payload!.Run.CurrentRoom.State.Should().Be("NodeSelected",
            because: "combat objectives select automatically when the party reaches their cell");

        return payload;
    }

    protected async Task CompleteActiveCombatAsync(Guid runId, Guid combatId)
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

        // Select the first available reward
        var pendingResponse = await Client.GetAsync(
            $"/api/v2/runs/{runId}/rewards/pending");

        if (pendingResponse.StatusCode == HttpStatusCode.OK)
        {
            var rewardOffer = await pendingResponse.Content
                .ReadFromJsonAsync<RewardOfferDto>();

            if (rewardOffer?.SelectedChoiceId is null && rewardOffer?.Choices.Count > 0)
            {
                var firstChoice = rewardOffer.Choices.First();

                var selectResponse = await Client.PostAsJsonAsync(
                    $"/api/v2/runs/{runId}/rewards/select",
                    new { ChoiceId = firstChoice.Id });

                selectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
    }

    private async Task ResolveEventChoiceIfRequiredAsync(
        Guid runId,
        ResolvedNodeEventOutcomeDto outcome)
    {
        var choiceId = outcome.RequiresPlayerChoice
            ? outcome.Choices.FirstOrDefault()?.ChoiceId
            : null;

        if (choiceId is null)
        {
            return;
        }

        var choiceResponse = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/current-event/choice",
            new { ChoiceId = choiceId });

        choiceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SelectPendingRewardIfAnyAsync(Guid runId)
    {
        var pendingResponse = await Client.GetAsync(
            $"/api/v2/runs/{runId}/rewards/pending");

        if (pendingResponse.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var rewardOffer = await pendingResponse.Content
            .ReadFromJsonAsync<RewardOfferDto>();

        if (rewardOffer?.SelectedChoiceId is null && rewardOffer?.Choices.Count > 0)
        {
            var firstChoice = rewardOffer.Choices.First();

            var selectResponse = await Client.PostAsJsonAsync(
                $"/api/v2/runs/{runId}/rewards/select",
                new { ChoiceId = firstChoice.Id });

            selectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    protected async Task<StartRunResponse> StartRunAsync()
    {
        var response = await Client.PostAsJsonAsync(
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
