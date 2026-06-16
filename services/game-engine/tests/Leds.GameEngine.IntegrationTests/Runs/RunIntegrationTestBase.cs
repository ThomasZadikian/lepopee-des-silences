using FluentAssertions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.SubmitCombatAction;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Rewards.Dtos;
using Leds.GameEngine.Application.Runs.GetRunById;
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

        await ResolveEventChoiceIfRequiredAsync(runId);
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

    protected async Task CompleteActiveCombatAsync(Guid runId, Guid combatId)
    {
        var combatResponse = await Client.GetAsync(
            $"/api/v2/runs/{runId}/combats/{combatId}");

        combatResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var combat = await combatResponse.Content
            .ReadFromJsonAsync<CombatInstanceDto>();

        combat.Should().NotBeNull();

        while (true)
        {
            var activeActor = combat!.Combatants.FirstOrDefault(c =>
                c.Id == combat.CurrentActorId &&
                c.Side == "Player" &&
                !c.IsDefeated);

            activeActor.Should().NotBeNull(
                because: "the legacy facade should expose the canonical active player combatant");

            var enemy = combat.Combatants.FirstOrDefault(c =>
                c.Side == "Enemy" &&
                !c.IsDefeated);

            enemy.Should().NotBeNull(
                because: "an in-progress combat should have at least one living enemy target");

            var actionResponse = await Client.PostAsJsonAsync(
                $"/api/v2/runs/{runId}/combats/{combatId}/actions",
                new { ActorId = activeActor!.Id, TargetId = enemy!.Id, ActionType = "BasicAttack" });

            var actionBody = await actionResponse.Content.ReadAsStringAsync();

            actionResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: actionBody);

            var actionResult = await actionResponse.Content
                .ReadFromJsonAsync<SubmitCombatActionResponse>();

            actionResult.Should().NotBeNull();

            if (actionResult!.Result.CombatState == "Completed" || actionResult.Run.ActiveCombatId is null)
            {
                break;
            }

            combatResponse = await Client.GetAsync(
                $"/api/v2/runs/{runId}/combats/{combatId}");

            var combatBody = await combatResponse.Content.ReadAsStringAsync();

            combatResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: combatBody);

            combat = await combatResponse.Content
                .ReadFromJsonAsync<CombatInstanceDto>();

            combat.Should().NotBeNull();
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

    private async Task ResolveEventChoiceIfRequiredAsync(Guid runId)
    {
        var getResponse = await Client.GetAsync($"/api/v2/runs/{runId}");

        if (getResponse.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var getPayload = await getResponse.Content
            .ReadFromJsonAsync<GetRunByIdResponse>();

        if (getPayload is null)
        {
            return;
        }

        var currentDepth = getPayload.Run.CurrentRoom.CurrentNodeDepth;

        var resolvedNode = getPayload.Run.CurrentRoom.Nodes
            .FirstOrDefault(node =>
                node.Row == currentDepth &&
                node.State == "Resolved" &&
                !node.HasChosenEventOption);

        if (resolvedNode is null)
        {
            return;
        }

        var choiceId = resolvedNode.Type switch
        {
            "Npc" => "listen",
            "Merchant" => "trade",
            "Law" => "accept-law",
            "Curse" => "accept-curse",
            _ => null
        };

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
