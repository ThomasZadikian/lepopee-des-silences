using FluentAssertions;
using Leds.GameEngine.Application.Runs.ExitMidRoom;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.ResumeRun;
using Leds.GameEngine.Application.Runs.SaveAndExitRun;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

[Collection("GameEngineApi")]
public sealed class ResumeRunEndpointTests : RunIntegrationTestBase
{
    public ResumeRunEndpointTests(GameEngineApiFactory factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task ExitMidRoom_AndResume_ShouldRestoreRoomToInitialState()
    {
        var startResponse = await StartRunAsync();
        var runId = startResponse.Run.Id;
        var initialGrid = startResponse.Run.CurrentRoom.Grid!;

        var nodeToChoose = FirstContactCombatNode(startResponse.Run.CurrentRoom);
        await MovePartyToNodeAsync(runId, nodeToChoose);

        // Act — exit mid-room
        var exitResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/exit-mid-room",
            content: null);
        var exitBody = await exitResponse.Content.ReadAsStringAsync();
        exitResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: exitBody);

        var exitPayload = await exitResponse.Content
            .ReadFromJsonAsync<ExitMidRoomResponse>();
        exitPayload.Should().NotBeNull();
        exitPayload!.Run.Status.Should().Be("Suspended");
        exitPayload.Run.CanResume.Should().BeTrue();

        // Act — resume the run
        var resumeResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/resume",
            content: null);
        var resumeBody = await resumeResponse.Content.ReadAsStringAsync();
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: resumeBody);

        var resumePayload = await resumeResponse.Content
            .ReadFromJsonAsync<ResumeRunResponse>();
        resumePayload.Should().NotBeNull();
        resumePayload!.Run.Status.Should().Be("Active");
        resumePayload.Run.CanResume.Should().BeFalse();

        // Mid-room exit deliberately rolls spatial exploration back to its entry snapshot.
        resumePayload.Run.CurrentRoom.State.Should().Be("Active");
        resumePayload.Run.CurrentRoom.Grid!.PartyX.Should().Be(initialGrid.PartyX);
        resumePayload.Run.CurrentRoom.Grid.PartyY.Should().Be(initialGrid.PartyY);
        resumePayload.Run.CurrentRoom.Nodes.Single(node => node.Id == nodeToChoose.Id)
            .State.Should().Be("Available");
    }

    [Fact]
    public async Task ExitMidRoom_AndResume_ShouldAllowContinuedPlay()
    {
        var startResponse = await StartRunAsync();
        var runId = startResponse.Run.Id;

        var firstNode = FirstContactCombatNode(startResponse.Run.CurrentRoom);
        await MovePartyToNodeAsync(runId, firstNode);

        var exitResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/exit-mid-room",
            content: null);
        exitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Resume
        var resumeResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/resume",
            content: null);
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resumePayload = await resumeResponse.Content
            .ReadFromJsonAsync<ResumeRunResponse>();
        resumePayload!.Run.Status.Should().Be("Active");

        // The encounter is available again after rollback and can be selected by contact.
        var resumedNode = resumePayload.Run.CurrentRoom.Nodes.Single(node => node.Id == firstNode.Id);
        await MovePartyToNodeAsync(runId, resumedNode);

        // The room should progress normally after resume
        var resolveResponse = await ResolveAndHandleCombatAsync(runId);
        resolveResponse.Run.CurrentRoom.State.Should().Be("NodeResolved");
    }

    [Fact]
    public async Task SaveAndExit_AndResume_ShouldRestoreFreeExplorationState()
    {
        var runId = (await StartRunAsync()).Run.Id;

        var saveResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/save-and-exit",
            content: null);
        var saveBody = await saveResponse.Content.ReadAsStringAsync();
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: saveBody);

        var savePayload = await saveResponse.Content
            .ReadFromJsonAsync<SaveAndExitRunResponse>();
        savePayload!.Run.Status.Should().Be("Suspended");
        savePayload.Run.CanResume.Should().BeTrue();

        // Act — resume
        var resumeResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/resume",
            content: null);
        var resumeBody = await resumeResponse.Content.ReadAsStringAsync();
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: resumeBody);

        var resumePayload = await resumeResponse.Content
            .ReadFromJsonAsync<ResumeRunResponse>();

        resumePayload!.Run.Status.Should().Be("Active");
        resumePayload.Run.CanResume.Should().BeFalse();
        resumePayload.Run.CurrentRoom.State.Should().Be("Active");
    }

    [Fact]
    public async Task Resume_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/resume",
            content: null);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, because: body);
        body.Should().Contain("Resource not found.");
        body.Should().Contain($"Run with id '{unknownRunId}' was not found.");
    }

    [Fact]
    public async Task Resume_ShouldReturnBadRequest_WhenRunIsActive()
    {
        var startResponse = await StartRunAsync();
        var runId = startResponse.Run.Id;

        var response = await Client.PostAsync(
            $"/api/v2/runs/{runId}/resume",
            content: null);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: body);
        body.Should().Contain("Domain rule violated.");
    }

    [Fact]
    public async Task Resume_ShouldReturnBadRequest_WhenRunIsAbandoned()
    {
        var runId = (await StartRunAsync()).Run.Id;

        var abandonResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/abandon",
            content: null);
        abandonResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resumeResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/resume",
            content: null);

        var body = await resumeResponse.Content.ReadAsStringAsync();
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: body);
        body.Should().Contain("Domain rule violated.");
    }

    [Fact]
    public async Task Resume_ShouldClearSavedAt()
    {
        var runId = (await StartRunAsync()).Run.Id;

        await Client.PostAsync(
            $"/api/v2/runs/{runId}/save-and-exit",
            content: null);

        var getBefore = await Client.GetAsync($"/api/v2/runs/{runId}");
        var beforePayload = await getBefore.Content
            .ReadFromJsonAsync<GetRunByIdResponse>();
        beforePayload!.Run.SavedAt.Should().NotBeNull();

        await Client.PostAsync(
            $"/api/v2/runs/{runId}/resume",
            content: null);

        var getAfter = await Client.GetAsync($"/api/v2/runs/{runId}");
        var afterPayload = await getAfter.Content
            .ReadFromJsonAsync<GetRunByIdResponse>();
        afterPayload!.Run.SavedAt.Should().BeNull();
    }

    [Fact]
    public async Task ExitMidRoom_ShouldSucceedAndReturnSuspended_WhenRunIsActive()
    {
        var startResponse = await StartRunAsync();
        var runId = startResponse.Run.Id;

        var response = await Client.PostAsync(
            $"/api/v2/runs/{runId}/exit-mid-room",
            content: null);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ExitMidRoomResponse>();
        payload!.Run.Status.Should().Be("Suspended");
        payload.Run.CanResume.Should().BeTrue();
        payload.Run.SavedAt.Should().NotBeNull();
        payload.Run.CurrentRoom.CurrentNodeDepth.Should().Be(0);
        payload.Run.CurrentRoom.State.Should().Be("Active");
    }

    [Fact]
    public async Task ExitMidRoom_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/exit-mid-room",
            content: null);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, because: body);
        body.Should().Contain("Resource not found.");
    }

    [Fact]
    public async Task ExitMidRoom_ShouldReturnBadRequest_WhenRunIsAlreadySuspended()
    {
        var runId = (await StartRunAsync()).Run.Id;

        var firstExit = await Client.PostAsync(
            $"/api/v2/runs/{runId}/exit-mid-room",
            content: null);
        firstExit.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await Client.PostAsync(
            $"/api/v2/runs/{runId}/exit-mid-room",
            content: null);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: body);
        body.Should().Contain("Domain rule violated.");
    }

    [Fact]
    public async Task SaveAndExit_ShouldReturnOk_AtFreeExplorationSafePoint()
    {
        var runId = (await StartRunAsync()).Run.Id;

        var response = await Client.PostAsync(
            $"/api/v2/runs/{runId}/save-and-exit",
            content: null);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<SaveAndExitRunResponse>();
        payload!.Run.Status.Should().Be("Suspended");
        payload.Run.CanResume.Should().BeTrue();
        payload.Run.SavedAt.Should().NotBeNull();
    }

}
