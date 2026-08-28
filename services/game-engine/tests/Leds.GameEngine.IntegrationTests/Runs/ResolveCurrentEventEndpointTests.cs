using FluentAssertions;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

[Collection("GameEngineApi")]
public sealed class ResolveCurrentEventEndpointTests : RunIntegrationTestBase
{
    public ResolveCurrentEventEndpointTests(GameEngineApiFactory factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldStartCombat_WhenEventIsCombat()
    {
        var (run, nodeToChoose) = await StartRunWithCombatNodeAsync();
        await MovePartyToNodeAsync(run.Id, nodeToChoose);

        var resolveResponse = await Client.PostAsync(
            $"/api/v2/runs/{run.Id}/current-event/resolve",
            content: null);

        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();

        resolveResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: resolveBody);

        var payload = await resolveResponse.Content.ReadFromJsonAsync<ResolveCurrentEventResponse>();

        payload.Should().NotBeNull(because: resolveBody);
        payload!.Run.Status.Should().Be("Active");
        payload.Run.ActiveCombatId.Should().NotBeNull(because: resolveBody);
        payload.Run.CurrentRoom.State.Should().Be("NodeSelected");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldResolveNode_AfterCombatCompleted()
    {
        var (run, nodeToChoose) = await StartRunWithCombatNodeAsync();
        await MovePartyToNodeAsync(run.Id, nodeToChoose);

        var resolvePayload = await ResolveAndHandleCombatAsync(run.Id);

        resolvePayload.Run.Status.Should().Be("Active");
        resolvePayload.Run.CurrentRoom.State.Should().Be("NodeResolved");

        var resolvedNode = resolvePayload.Run.CurrentRoom.Nodes
            .Single(node => node.Id == nodeToChoose.Id);

        resolvedNode.State.Should().Be("Resolved");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldReturnBadRequest_WhenNoNodeWasSelected()
    {
        var startRunResponse = await StartRunAsync();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/current-event/resolve",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Domain rule violated.");
        body.Should().Contain("No node has been selected");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/current-event/resolve",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Resource not found.");
        body.Should().Contain($"Run with id '{unknownRunId}' was not found.");
    }
}
