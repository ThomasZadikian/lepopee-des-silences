using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.Player.Application.Players;
using Leds.Player.Application.Players.CreatePlayerProfile;

namespace Leds.Player.IntegrationTests.Controllers;

[Collection("PlayerApi")]
public sealed class PlayersControllerTests
{
    private readonly HttpClient _client;

    public PlayersControllerTests(PlayerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostPlayers_ShouldCreatePlayerProfile()
    {
        var response = await _client.PostAsJsonAsync("/api/v2/players",
            new { DisplayName = "Test Player" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<CreatePlayerProfileResponse>();
        payload.Should().NotBeNull();
        payload!.Profile.DisplayName.Should().Be("Test Player");
        payload.Profile.Characters.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPlayerById_ShouldReturnCreatedProfile()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v2/players",
            new { DisplayName = "GET Test" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePlayerProfileResponse>();

        var response = await _client.GetAsync($"/api/v2/players/{created!.Profile.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<PlayerProfileDto>();
        profile!.DisplayName.Should().Be("GET Test");
    }

    [Fact]
    public async Task GetPlayerById_ShouldReturnNotFound_WhenMissing()
    {
        var response = await _client.GetAsync($"/api/v2/players/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRunSnapshot_ShouldReturnDefaultCharacter()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v2/players",
            new { DisplayName = "Snapshot Test" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePlayerProfileResponse>();

        var response = await _client.GetAsync($"/api/v2/players/{created!.Profile.Id}/run-snapshot");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await response.Content.ReadFromJsonAsync<PlayerRunSnapshotResponse>();
        snapshot.Should().NotBeNull();
        snapshot!.Characters.Should().HaveCount(1);
        snapshot.Characters.Single().SkillKeys.Should().Contain("skill.basic.strike");
        snapshot.Characters.Single().MaxVitality.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRunSnapshot_ShouldReturnNotFound_WhenPlayerMissing()
    {
        var response = await _client.GetAsync($"/api/v2/players/{Guid.NewGuid()}/run-snapshot");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePlayerThenGetRunSnapshot_ShouldReturnCharacterUsableByGameEngine()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v2/players",
            new { DisplayName = "Integration Test" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePlayerProfileResponse>();

        var snapshotResponse = await _client.GetAsync(
            $"/api/v2/players/{created!.Profile.Id}/run-snapshot");

        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<PlayerRunSnapshotResponse>();

        snapshot.Should().NotBeNull();
        snapshot!.PlayerId.Should().Be(created.Profile.Id);
        snapshot.Characters.Should().NotBeEmpty();

        var character = snapshot.Characters.First();
        character.DefinitionKey.Should().NotBeNullOrWhiteSpace();
        character.MaxVitality.Should().BeGreaterThan(0);
        character.SkillKeys.Should().Contain("skill.basic.strike");
    }
}
