using FluentAssertions;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

[Collection("GameEngineApi")]
public sealed class ResolveCurrentEventOutcomeEndpointTests : RunIntegrationTestBase
{
    public ResolveCurrentEventOutcomeEndpointTests(
        GameEngineApiFactory factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldReturnOutcome_WhenNodeIsSelected()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;
        var node = FirstContactCombatNode(startRunResponse.Run.CurrentRoom);
        var nodeId = node.Id;
        await MovePartyToNodeAsync(runId, node);

        var resolveResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/current-event/resolve",
            content: null);

        var resolveBody = await resolveResponse.Content.ReadAsStringAsync();

        resolveResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: resolveBody);

        var payload = await resolveResponse.Content
            .ReadFromJsonAsync<ResolveCurrentEventResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().Be(runId);

        payload.Outcome.NodeId.Should().Be(nodeId);
        payload.Outcome.EventTypes.Should().NotBeEmpty();
        payload.Outcome.PrimaryEventType.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.ResolutionKind.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.Title.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.Description.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.RiskLevel.Should().BeInRange(0, 100);
        payload.Outcome.RewardProfile.Should().NotBeNullOrWhiteSpace();
        payload.Outcome.NarrativeFragments.Should().NotBeNull();
    }

}
