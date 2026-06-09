using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Application.Runs.ExitMidRoom;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Application.Runs.ResumeRun;
using Leds.GameEngine.Application.Runs.SaveAndExitRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class ResumeRunEndpointTests : RunIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public ResumeRunEndpointTests(WebApplicationFactory<Program> factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task ExitMidRoom_AndResume_ShouldRestoreRoomToInitialState()
    {
        // Arrange — start a run and progress into the room
        var startResponse = await StartRunAsync();
        var runId = startResponse.Run.Id;

        var nodeToChoose = startResponse.Run.CurrentRoom.AvailableNodes.First();
        var chooseResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{nodeToChoose.Id}/choose",
            content: null);
        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

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

        // Assert — room should be reset to initial state
        resumePayload.Run.CurrentRoom.CurrentNodeDepth.Should().Be(0);
        resumePayload.Run.CurrentRoom.State.Should().Be("Active");
        resumePayload.Run.CurrentRoom.AvailableNodes.Should()
            .OnlyContain(n => n.Row == 0 && n.State == "Available");
    }

    [Fact]
    public async Task ExitMidRoom_AndResume_ShouldAllowContinuedPlay()
    {
        // Arrange — start run, pick a node, exit mid-room
        var startResponse = await StartRunAsync();
        var runId = startResponse.Run.Id;

        var firstNode = startResponse.Run.CurrentRoom.AvailableNodes.First();
        await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{firstNode.Id}/choose",
            content: null);

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

        // Act — continue playing: choose a node after resume
        var resumedNode = resumePayload.Run.CurrentRoom.AvailableNodes.First();
        var chooseAgain = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{resumedNode.Id}/choose",
            content: null);
        chooseAgain.StatusCode.Should().Be(HttpStatusCode.OK);

        // The room should progress normally after resume
        var resolveResponse = await ResolveAndHandleCombatAsync(runId);
        resolveResponse.Run.CurrentRoom.State.Should().Be("NodeResolved");
    }

    [Fact]
    public async Task SaveAndExit_AndResume_ShouldRestoreRoomResolved_WhenRunWasRoomCleared()
    {
        // Arrange — drive run to RoomResolved and save
        var runId = await StartRunAtRoomResolvedAsync();

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

        // Assert — run returns to RoomResolved
        resumePayload!.Run.Status.Should().Be("RoomResolved");
        resumePayload.Run.CanResume.Should().BeFalse();
        resumePayload.Run.CurrentRoom.State.Should().Be("Completed");
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
        var runId = await StartRunAtRoomResolvedAsync();

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
        var runId = await StartRunAtRoomResolvedAsync();

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
    public async Task ExitMidRoom_ShouldReturnBadRequest_WhenRunIsRoomResolved()
    {
        var runId = await StartRunAtRoomResolvedAsync();

        var response = await Client.PostAsync(
            $"/api/v2/runs/{runId}/exit-mid-room",
            content: null);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: body);
        body.Should().Contain("Domain rule violated.");
    }

    [Fact]
    public async Task SaveAndExit_ShouldReturnOk_WhenRunIsRoomResolved()
    {
        var runId = await StartRunAtRoomResolvedAsync();

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

    /// <summary>
    /// Plays through the first room (all nodes + boss + reward) and returns
    /// the run Id in <see cref="RunStatus.RoomResolved"/> (room cleared).
    /// </summary>
    private async Task<Guid> StartRunAtRoomResolvedAsync()
    {
        var startRunResponse = await StartRunAsync();
        var runId = startRunResponse.Run.Id;
        var currentRoom = startRunResponse.Run.CurrentRoom;

        while (currentRoom.CurrentNodeDepth < currentRoom.MaxNodeDepth)
        {
            var nodeToChoose = currentRoom.AvailableNodes.First();

            var chooseResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/nodes/{nodeToChoose.Id}/choose",
                content: null);
            chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var resolved = await ResolveAndHandleCombatAsync(runId);

            var progressResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/progress",
                content: null);
            progressResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var progressBody = await progressResponse.Content
                .ReadFromJsonAsync<ProgressRunResponse>();
            currentRoom = progressBody!.Run.CurrentRoom;
        }

        var bossNode = currentRoom.AvailableNodes.Single();
        bossNode.IsBoss.Should().BeTrue();

        var chooseBoss = await Client.PostAsync(
            $"/api/v2/runs/{runId}/nodes/{bossNode.Id}/choose",
            content: null);
        chooseBoss.StatusCode.Should().Be(HttpStatusCode.OK);

        var bossResolved = await ResolveAndHandleCombatAsync(runId);
        bossResolved.Run.Status.Should().Be("RoomResolved");

        return runId;
    }
}
