using FluentAssertions;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class ResolveCurrentEventEndpointTests : RunIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public ResolveCurrentEventEndpointTests(WebApplicationFactory<Program> factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldStartCombat_WhenEventIsCombat()
    {
        var startRunResponse = await StartRunAsync();
        var nodeToChoose = startRunResponse.Run.CurrentRoom.AvailableNodes
            .FirstOrDefault(node => node.Type == "Combat");

        if (nodeToChoose is null)
        {
            return;
        }

        var chooseResponse = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{nodeToChoose.Id}/choose",
            content: null);

        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolveResponse = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/current-event/resolve",
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
        var startRunResponse = await StartRunAsync();
        var nodeToChoose = startRunResponse.Run.CurrentRoom.AvailableNodes.First();

        var chooseResponse = await Client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/nodes/{nodeToChoose.Id}/choose",
            content: null);

        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolvePayload = await ResolveAndHandleCombatAsync(startRunResponse.Run.Id);

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
        body.Should().Contain("No node has been selected for the current room depth.");
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
