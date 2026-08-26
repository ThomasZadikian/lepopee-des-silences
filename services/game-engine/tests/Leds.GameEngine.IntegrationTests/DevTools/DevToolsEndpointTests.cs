using FluentAssertions;
using Leds.GameEngine.Application.DevTools;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;

namespace Leds.GameEngine.IntegrationTests.DevTools;

[Collection("GameEngineApi")]
public sealed class DevToolsEndpointTests
{
    private const string Token = "local-devtools-token";
    private readonly GameEngineApiFactory _factory;

    public DevToolsEndpointTests(GameEngineApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Status_ShouldReturnNotFound_WhenEnvironmentIsProduction()
    {
        using var client = CreateClient(environment: "Production", enabled: true, includeToken: true);

        var response = await client.GetAsync("/api/dev/v2/status");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Status_ShouldReturnNotFound_WhenDevToolsDisabled()
    {
        using var client = CreateClient(environment: "Development", enabled: false, includeToken: true);

        var response = await client.GetAsync("/api/dev/v2/status");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Status_ShouldReturnForbidden_WhenTokenMissing()
    {
        using var client = CreateClient(environment: "Development", enabled: true, includeToken: false);

        var response = await client.GetAsync("/api/dev/v2/status");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Status_ShouldReturnForbidden_WhenTokenInvalid()
    {
        using var client = CreateClient(environment: "Development", enabled: true, includeToken: true, token: "wrong-token");

        var response = await client.GetAsync("/api/dev/v2/status");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Status_ShouldReturnOk_WhenDevelopmentEnabledAndTokenValid()
    {
        using var client = CreateClient(environment: "Development", enabled: true, includeToken: true);

        var response = await client.GetAsync("/api/dev/v2/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DevToolsStatusPayload>();
        payload.Should().NotBeNull();
        payload!.Enabled.Should().BeTrue();
        payload.Environment.Should().Be("Development");
    }

    [Fact]
    public async Task ForcePalaceRoomState_ShouldUpdateOnlyCurrentRoom()
    {
        using var client = CreateClient(environment: "Development", enabled: true, includeToken: true);
        var run = await StartRunAsync(client);
        var initialRoomId = run.Run.CurrentRoom.Id;

        var response = await client.PostAsJsonAsync(
            $"/api/dev/v2/runs/{run.Run.Id}/current-room/palace-state",
            new { State = "Silent" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DevToolsRunDebugResult>();

        payload.Should().NotBeNull();
        payload!.Run.CurrentRoom.Id.Should().Be(initialRoomId);
        payload.Run.CurrentRoom.PalaceState!.Key.Should().Be("silent");
        payload.Run.Rooms.Single(room => room.Id == initialRoomId).PalaceState!.Key.Should().Be("silent");
    }

    [Fact]
    public async Task ForceRoomClimate_ShouldExposeCurrentRoomClimate()
    {
        using var client = CreateClient(environment: "Development", enabled: true, includeToken: true);
        var run = await StartRunAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/dev/v2/runs/{run.Run.Id}/current-room/climate",
            new { Climate = "Heatwave" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DevToolsRunDebugResult>();

        payload.Should().NotBeNull();
        payload!.Run.CurrentRoom.ActiveClimate.Should().Be("Heatwave");
        payload.Run.ActiveModifiers.Should().ContainSingle(modifier =>
            modifier.Type == "RoomClimate" &&
            modifier.SourceType == "DevTools" &&
            modifier.Value == 3);
    }

    [Fact]
    public async Task ActivateLaw_ShouldBeIdempotent_AndClearLawsShouldRemoveActiveLaws()
    {
        using var client = CreateClient(environment: "Development", enabled: true, includeToken: true);
        var run = await StartRunAsync(client);

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/dev/v2/runs/{run.Run.Id}/laws/activate",
            new { LawKey = "law-silence-v1" });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await client.PostAsJsonAsync(
            $"/api/dev/v2/runs/{run.Run.Id}/laws/activate",
            new { LawKey = "law-silence-v1" });
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<DevToolsRunDebugResult>();
        secondPayload.Should().NotBeNull();
        secondPayload!.Run.ActivePalaceLaws.Count(law => law.Key == "law-silence-v1").Should().Be(1);

        var clearResponse = await client.PostAsync($"/api/dev/v2/runs/{run.Run.Id}/laws/clear", null);
        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var clearPayload = await clearResponse.Content.ReadFromJsonAsync<DevToolsRunDebugResult>();
        clearPayload.Should().NotBeNull();
        clearPayload!.Run.ActivePalaceLaws.Should().BeEmpty();
    }

    [Fact]
    public async Task AdvanceRooms_ShouldUseNormalRoomGenerationPipeline()
    {
        using var client = CreateClient(environment: "Development", enabled: true, includeToken: true);
        var run = await StartRunAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/dev/v2/runs/{run.Run.Id}/advance-rooms",
            new { Count = 2 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DevToolsRunDebugResult>();

        payload.Should().NotBeNull();
        payload!.Run.CurrentDepth.Should().Be(2);
        payload.Run.Rooms.Should().ContainSingle()
            .Which.Id.Should().Be(payload.Run.CurrentRoom.Id,
                because: "the runtime DTO intentionally exposes only the current spatial room");
        payload.Run.MarkovMatrixVersion.Should().Be(run.Run.MarkovMatrixVersion);
    }

    private HttpClient CreateClient(
        string environment,
        bool enabled,
        bool includeToken,
        string? token = null)
    {
        _factory.ResetDatabase();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DevTools:Enabled"] = enabled.ToString(),
                    ["DevTools:Token"] = Token
                });
            });
        }).CreateClient();

        if (includeToken)
            client.DefaultRequestHeaders.Add("X-Leds-DevTools-Token", token ?? Token);

        return client;
    }

    private static async Task<StartRunResponse> StartRunAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v2/runs",
            new { PlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111") });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();
        payload.Should().NotBeNull();
        return payload!;
    }

    private sealed record DevToolsStatusPayload(bool Enabled, string Environment);
}
