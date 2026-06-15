using FluentAssertions;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.MoveToNextRoom;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class PlayableSliceTests : RunIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public PlayableSliceTests(WebApplicationFactory<Program> factory)
        : base(factory.CreateClient())
    {
    }

    [Fact]
    public async Task FullBackendLoop_ShouldCompleteMultipleRooms()
    {
        var startRunResponse = await StartRunAsync();

        var runId = startRunResponse.Run.Id;

        // === Complete first room (depth 0) ===
        var firstRoom = startRunResponse.Run.CurrentRoom;

        firstRoom.Depth.Should().Be(0);
        firstRoom.State.Should().Be("Active");
        firstRoom.AvailableNodes.Should().NotBeEmpty();

        var completedFirstRoom = await CompleteRoomAsync(runId, firstRoom);

        ((string)completedFirstRoom.State).Should().Be("Completed");

        var firstRoomGet = await Client.GetAsync($"/api/v2/runs/{runId}");
        var firstRoomPayload = await firstRoomGet.Content
            .ReadFromJsonAsync<GetRunByIdResponse>();

        firstRoomPayload.Should().NotBeNull();
        firstRoomPayload!.Run.Status.Should().Be("RoomResolved");
        firstRoomPayload.Run.CurrentDepth.Should().Be(0);

        // === Enter interlude ===
        var enterInterludeResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/interlude/enter",
            content: null);

        var enterInterludeBody = await enterInterludeResponse.Content.ReadAsStringAsync();
        enterInterludeResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: enterInterludeBody);

        // === Move to next room (depth 1) ===
        var moveResponse = await Client.PostAsync(
            $"/api/v2/runs/{runId}/rooms/next",
            content: null);

        moveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var movePayload = await moveResponse.Content
            .ReadFromJsonAsync<MoveToNextRoomResponse>();

        movePayload.Should().NotBeNull();
        movePayload!.Run.Status.Should().Be("Active");
        movePayload.Run.CurrentDepth.Should().Be(1);
        movePayload.Run.CurrentRoom.Depth.Should().Be(1);
        movePayload.Run.CurrentRoom.State.Should().Be("Active");
        movePayload.Run.CurrentRoom.AvailableNodes.Should().NotBeEmpty();

        // === Complete second room (depth 1) ===
        var secondRoom = movePayload.Run.CurrentRoom;

        var completedSecondRoom = await CompleteRoomAsync(runId, secondRoom);

        ((string)completedSecondRoom.State).Should().Be("Completed");

        var secondRoomGet = await Client.GetAsync($"/api/v2/runs/{runId}");
        var secondRoomPayload = await secondRoomGet.Content
            .ReadFromJsonAsync<GetRunByIdResponse>();

        secondRoomPayload.Should().NotBeNull();
        secondRoomPayload!.Run.Status.Should().Be("RoomResolved");
        secondRoomPayload.Run.CurrentDepth.Should().Be(1);
        secondRoomPayload.Run.CurrentRoom.State.Should().Be("Completed");
    }

    private async Task<dynamic> CompleteRoomAsync(Guid runId, dynamic currentRoom)
    {
        while ((string)currentRoom.State != "Completed")
        {
            var nodeToChoose = currentRoom.AvailableNodes[0];

            var chooseResponse = await Client.PostAsync(
                $"/api/v2/runs/{runId}/nodes/{nodeToChoose.Id}/choose",
                content: null);

            var chooseBody = await chooseResponse.Content.ReadAsStringAsync();

            chooseResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: chooseBody);

            var resolvedPayload = await ResolveAndHandleCombatAsync(runId);

            currentRoom = resolvedPayload.Run.CurrentRoom;

            if ((string)currentRoom.State == "Completed")
            {
                return currentRoom;
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

        return currentRoom;
    }
}