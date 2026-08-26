using FluentAssertions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.TacticalCombat;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Rewards.Dtos;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Application.Runs.ConfirmRoomExit;
using Leds.GameEngine.Application.Runs.EnterGridNode;
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

    protected static MapNodeDto FirstConfirmableNode(RoomDto room) =>
        room.Nodes
            .Where(node =>
                node.State == "Available"
                && node.Type is not "Exit"
                && node.ContactBehavior == "None")
            .Select(node => (Node: node, Path: FindSafePath(room, node)))
            .Where(candidate => candidate.Path is not null)
            .OrderBy(candidate => candidate.Path!.Count)
            .Select(candidate => candidate.Node)
            .First();

    protected async Task<MovePartyResponse> MovePartyAsync(Guid runId, MapNodeDto node)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/party/move",
            new { TargetX = node.Lane, TargetY = node.Row });
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<MovePartyResponse>();
        payload.Should().NotBeNull(because: body);
        return payload!;
    }

    private async Task<MovePartyResponse> MovePartySafelyAsync(
        Guid runId,
        RoomDto room,
        MapNodeDto node)
    {
        var path = FindSafePath(room, node)
            ?? throw new InvalidOperationException(
                $"Node '{node.Id}' cannot be reached without crossing another contact trigger.");

        MovePartyResponse? payload = null;
        foreach (var (x, y) in path)
        {
            payload = await MovePartyAsync(
                runId,
                node with { Lane = x, Row = y });
        }

        return payload
            ?? throw new InvalidOperationException("The party is already standing on the target node.");
    }

    protected async Task<MovePartyResponse> MovePartyToNodeAsync(Guid runId, MapNodeDto node)
    {
        var run = await GetRunAsync(runId);
        var payload = await MovePartySafelyAsync(runId, run.CurrentRoom, node);
        payload.Run.CurrentRoom.State.Should().Be("NodeSelected",
            because: "combat objectives select automatically when the party reaches their cell");

        return payload;
    }

    protected async Task<EnterGridNodeResponse> MovePartyAndEnterNodeAsync(
        Guid runId,
        MapNodeDto node)
    {
        var run = await GetRunAsync(runId);
        await MovePartySafelyAsync(runId, run.CurrentRoom, node);

        var response = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{node.Id}/enter",
            content: null);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<EnterGridNodeResponse>();
        payload.Should().NotBeNull(because: body);
        payload!.Run.CurrentRoom.State.Should().Be("NodeSelected");
        return payload;
    }

    protected async Task<(RunDto Run, MapNodeDto Node)> StartRunWithCombatNodeAsync()
    {
        var run = (await StartRunAsync()).Run;

        for (var roomAttempt = 0; roomAttempt < 10; roomAttempt++)
        {
            var combatNode = run.CurrentRoom.Nodes
                .Where(node =>
                    node.State == "Available"
                    && node.ContactBehavior is "TriggerOnEnter" or "Blocking"
                    && node.Type is "Combat" or "Elite" or "Rare" or "RoomBoss" or "FinalBoss")
                .Select(node => (Node: node, Path: FindSafePath(run.CurrentRoom, node)))
                .Where(candidate => candidate.Path is not null)
                .OrderBy(candidate => candidate.Path!.Count)
                .Select(candidate => candidate.Node)
                .FirstOrDefault();
            if (combatNode is not null)
            {
                return (run, combatNode);
            }

            var exitNode = run.CurrentRoom.Nodes.First(node => node.Type == "Exit");
            await MovePartySafelyAsync(run.Id, run.CurrentRoom, exitNode);

            var exitResponse = await Client.PostAsync(
                $"/api/v2/runs/{run.Id}/nodes/{exitNode.Id}/exit",
                content: null);
            var exitBody = await exitResponse.Content.ReadAsStringAsync();
            exitResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: exitBody);

            var exited = await exitResponse.Content.ReadFromJsonAsync<ConfirmRoomExitResponse>();
            exited.Should().NotBeNull(because: exitBody);
            run = exited!.Run;
        }

        throw new InvalidOperationException("The generated run did not expose a combat room.");
    }

    protected async Task<RunDto> GetRunAsync(Guid runId)
    {
        var response = await Client.GetAsync($"/api/v2/runs/{runId}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<GetRunByIdResponse>();
        payload.Should().NotBeNull(because: body);
        return payload!.Run;
    }

    private static IReadOnlyList<(int X, int Y)>? FindSafePath(RoomDto room, MapNodeDto target)
    {
        var grid = room.Grid!;
        var start = (X: grid.PartyX, Y: grid.PartyY);
        var destination = (X: target.Lane, Y: target.Row);
        var obstacles = grid.ObstacleCells.Select(cell => (X: cell[0], Y: cell[1])).ToHashSet();
        var triggers = room.Nodes
            .Where(node =>
                node.Id != target.Id
                && node.State == "Available"
                && node.ContactBehavior is "TriggerOnEnter" or "Blocking")
            .Select(node => (X: node.Lane, Y: node.Row))
            .ToHashSet();
        var queue = new Queue<(int X, int Y)>();
        var previous = new Dictionary<(int X, int Y), (int X, int Y)>();
        var visited = new HashSet<(int X, int Y)> { start };
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == destination)
            {
                var path = new List<(int X, int Y)>();
                while (current != start)
                {
                    path.Add(current);
                    current = previous[current];
                }

                path.Reverse();
                return path;
            }

            foreach (var next in new[]
                     {
                         (current.X + 1, current.Y),
                         (current.X - 1, current.Y),
                         (current.X, current.Y + 1),
                         (current.X, current.Y - 1)
                     })
            {
                var index = (next.Item2 * grid.Width) + next.Item1;
                if (next.Item1 < 0 || next.Item1 >= grid.Width
                    || next.Item2 < 0 || next.Item2 >= grid.Height
                    || !grid.FloorCells[index]
                    || obstacles.Contains(next)
                    || triggers.Contains(next)
                    || !visited.Add(next))
                {
                    continue;
                }

                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        return null;
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

                var selectBody = await selectResponse.Content.ReadAsStringAsync();
                selectResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: selectBody);
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

            var selectBody = await selectResponse.Content.ReadAsStringAsync();
            selectResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: selectBody);
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
