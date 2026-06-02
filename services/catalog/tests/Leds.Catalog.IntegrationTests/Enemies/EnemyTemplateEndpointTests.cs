using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.Catalog.IntegrationTests.Enemies;

public sealed class EnemyTemplateEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EnemyTemplateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListEnemies_ShouldReturnActiveEnemyTemplates()
    {
        var response = await _client.GetAsync("/api/v2/catalog/enemies");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListEnemyTemplatesResponse>();

        payload.Should().NotBeNull();
        payload!.Templates.Should().NotBeEmpty();
        payload.Templates.Should().OnlyContain(template =>
            template.Status == "Active");

        payload.Templates.Select(template => template.Key)
            .Should()
            .Contain("enemy-shadow-wolf");
    }

    [Fact]
    public async Task GetEnemyByKey_ShouldReturnEnemyTemplate_WhenKeyExists()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemies/enemy-shadow-wolf");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetEnemyTemplateResponse>();

        payload.Should().NotBeNull();
        payload!.Template.Should().NotBeNull();

        payload.Template!.Key.Should().Be("enemy-shadow-wolf");
        payload.Template.Name.Should().Be("Loup d’Ombre");
        payload.Template.Status.Should().Be("Active");
        payload.Template.Archetype.Should().Be("Trauma");
        payload.Template.Element.Should().Be("Shadow");
    }

    [Fact]
    public async Task GetEnemyByKey_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemies/unknown-enemy");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEnemyByKey_ShouldReturnBadRequest_WhenKeyIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/enemies/%20%20%20");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Enemy template key is required.");
    }

    private sealed record ListEnemyTemplatesResponse(
        IReadOnlyCollection<EnemyTemplateDto> Templates);

    private sealed record GetEnemyTemplateResponse(
        EnemyTemplateDto? Template);

    private sealed record EnemyTemplateDto(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string Archetype,
        string Element,
        int MaxHealth,
        int Strength,
        int Intelligence,
        int Speed,
        int PhysicalResistance,
        int MagicalResistance,
        int ExperienceReward,
        int GoldReward);
}