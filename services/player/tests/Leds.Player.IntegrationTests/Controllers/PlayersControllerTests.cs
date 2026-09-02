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
    public async Task PostPlayers_ShouldCreateProfileWithoutCharacterBeforeOnboarding()
    {
        var response = await _client.PostAsJsonAsync("/api/v2/players", new { DisplayName = "Test Player" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<CreatePlayerProfileResponse>();
        payload.Should().NotBeNull();
        payload!.Profile.DisplayName.Should().Be("Test Player");
        payload.Profile.Characters.Should().BeEmpty();
    }

    [Fact]
    public async Task PostCharacter_ShouldCreateSelectedArchetype()
    {
        var created = await CreatePlayer("Character Test");

        var response = await _client.PostAsJsonAsync(
            $"/api/v2/players/{created.Profile.Id}/characters",
            new { DisplayName = "Aster", ArchetypeKey = "archetype.porteur" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var profile = await response.Content.ReadFromJsonAsync<PlayerProfileDto>();
        var character = profile!.Characters.Should().ContainSingle().Subject;
        character.DisplayName.Should().Be("Aster");
        character.ArchetypeKey.Should().Be("archetype.porteur");
        character.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlayerById_ShouldReturnCreatedProfile()
    {
        var created = await CreatePlayer("GET Test");

        var response = await _client.GetAsync($"/api/v2/players/{created.Profile.Id}");

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
    public async Task GetRunSnapshot_ShouldReturnConflictUntilCharacterIsSelected()
    {
        var created = await CreatePlayer("Snapshot Empty");

        var response = await _client.GetAsync($"/api/v2/players/{created.Profile.Id}/run-snapshot");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetRunSnapshot_ShouldReturnSelectedCharacter()
    {
        var created = await CreatePlayerWithCharacter("Snapshot Test", "Aster");

        var response = await _client.GetAsync($"/api/v2/players/{created.Profile.Id}/run-snapshot");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await response.Content.ReadFromJsonAsync<PlayerRunSnapshotResponse>();
        snapshot.Should().NotBeNull();
        snapshot!.Characters.Should().HaveCount(1);
        snapshot.Characters.Single().DisplayName.Should().Be("Aster");
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
    public async Task CreatePlayerThenSelectCharacter_ShouldReturnCharacterUsableByGameEngine()
    {
        var created = await CreatePlayerWithCharacter("Integration Test", "Nocturne");

        var snapshotResponse = await _client.GetAsync($"/api/v2/players/{created.Profile.Id}/run-snapshot");

        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<PlayerRunSnapshotResponse>();
        snapshot.Should().NotBeNull();
        snapshot!.PlayerId.Should().Be(created.Profile.Id);
        var character = snapshot.Characters.Should().ContainSingle().Subject;
        character.DefinitionKey.Should().Be("character.player.self");
        character.MaxVitality.Should().BeGreaterThan(0);
        character.SkillKeys.Should().Contain("skill.basic.strike");
    }

    private async Task<CreatePlayerProfileResponse> CreatePlayer(string displayName)
    {
        var response = await _client.PostAsJsonAsync("/api/v2/players", new { DisplayName = displayName });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatePlayerProfileResponse>())!;
    }

    private async Task<CreatePlayerProfileResponse> CreatePlayerWithCharacter(string accountName, string characterName)
    {
        var created = await CreatePlayer(accountName);
        var characterResponse = await _client.PostAsJsonAsync(
            $"/api/v2/players/{created.Profile.Id}/characters",
            new { DisplayName = characterName, ArchetypeKey = "archetype.porteur" });
        characterResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        return created;
    }
}
