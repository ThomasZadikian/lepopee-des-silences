using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Leds.Catalog.IntegrationTests.Enemies.Definitions;

public sealed class EnemyDefinitionEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EnemyDefinitionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListEnemyDefinitions_ShouldReturn200()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListEnemyDefinitions_ShouldReturnSeededEnemies()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions");

        var payload = await response.Content
            .ReadFromJsonAsync<ListEnemyDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().OnlyContain(d => d.Status == "Active");
        payload.Definitions.Select(d => d.Key)
            .Should()
            .Contain("canon.enemy.sentinelle-seuil")
            .And.Contain("canon.enemy.imperatrice");
    }

    [Fact]
    public async Task GetEnemyDefinitionByKey_ShouldReturnExpectedEnemy()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions/canon.enemy.sentinelle-seuil");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK, because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetEnemyDefinitionResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();

        payload.Definition!.Key.Should().Be("canon.enemy.sentinelle-seuil");
        payload.Definition.Name.Should().Be("Sentinelle du Seuil");
        payload.Definition.Status.Should().Be("Active");
        payload.Definition.Archetype.Should().Be("Bruiser");
        payload.Definition.BaseDifficulty.Should().Be(2);
        payload.Definition.MinRiskLevel.Should().Be(1);
        payload.Definition.MaxRiskLevel.Should().Be(4);
        payload.Definition.CompatibleRoomTypes.Should().Contain("Silence");
        payload.Definition.Tags.Should().Contain("silence");
        payload.Definition.SkillKeys.Should().Contain("canon.skill.flamme-froide");
    }

    [Fact]
    public async Task GetEnemyDefinitionByKey_ShouldReturn404_WhenUnknown()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions/unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEnemyDefinitionByKey_ShouldReturn400_WhenKeyIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions/%20%20%20");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest, because: body);

        body.Should().Contain("Enemy definition key is required.");
    }

    [Fact]
    public async Task GetEnemyDefinitionsByRoomType_ShouldReturnThresholdEnemies()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions/room-type/Threshold");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK, because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListEnemyDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().AllSatisfy(d =>
            d.CompatibleRoomTypes.Should().Contain("Threshold"));
    }

    [Fact]
    public async Task GetEnemyDefinitionsByRoomType_ShouldReturnSilenceEnemies()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions/room-type/Silence");

        var payload = await response.Content
            .ReadFromJsonAsync<ListEnemyDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Select(d => d.Key)
            .Should()
            .Contain("canon.enemy.choeur-muet")
            .And.Contain("canon.enemy.page-inachevee");
    }

    [Fact]
    public async Task GetCompatibleEnemyDefinitions_ShouldReturnFilteredResults()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions/compatible?roomType=Silence&riskLevel=3");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK, because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListEnemyDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().OnlyContain(d =>
            d.MinRiskLevel <= 3 && 3 <= d.MaxRiskLevel);
    }

    [Fact]
    public async Task GetCompatibleEnemyDefinitions_ShouldReturn400_WhenRiskLevelIsZero()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions/compatible?roomType=Silence&riskLevel=0");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest, because: body);

        body.Should().Contain("Risk level must be at least 1.");
    }

    [Fact]
    public async Task GetCompatibleEnemyDefinitions_ShouldReturnEmpty_WhenRoomTypeUnknown()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions/compatible?roomType=Unknown&riskLevel=3");

        var payload = await response.Content
            .ReadFromJsonAsync<ListEnemyDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().BeEmpty();
    }

    [Fact]
    public async Task FinalEnemies_ShouldBeExposed()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemy-definitions/room-type/Final");

        var payload = await response.Content
            .ReadFromJsonAsync<ListEnemyDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Select(d => d.Key)
            .Should()
            .Contain("canon.enemy.himlit");
    }

    private sealed record ListEnemyDefinitionsResponse(
        IReadOnlyCollection<EnemyDefinitionDto> Definitions);

    private sealed record GetEnemyDefinitionResponse(
        EnemyDefinitionDto? Definition);

    private sealed record EnemyDefinitionDto(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string Archetype,
        int BaseDifficulty,
        int MinRiskLevel,
        int MaxRiskLevel,
        IReadOnlyCollection<string> CompatibleRoomTypes,
        IReadOnlyCollection<string> Tags,
        IReadOnlyCollection<string> SkillKeys);
}
