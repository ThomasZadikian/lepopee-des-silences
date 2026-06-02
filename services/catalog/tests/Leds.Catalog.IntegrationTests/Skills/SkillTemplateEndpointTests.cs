using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.Catalog.IntegrationTests.Skills;

public sealed class SkillTemplateEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SkillTemplateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListSkills_ShouldReturnActiveSkillTemplates()
    {
        var response = await _client.GetAsync("/api/v2/catalog/skills");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListSkillTemplatesResponse>();

        payload.Should().NotBeNull();
        payload!.Templates.Should().NotBeEmpty();
        payload.Templates.Should().OnlyContain(template =>
            template.Status == "Active");

        payload.Templates.Select(template => template.Key)
            .Should()
            .Contain("skill-shadow-bite");
    }

    [Fact]
    public async Task GetSkillByKey_ShouldReturnSkillTemplate_WhenKeyExists()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skills/skill-shadow-bite");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetSkillTemplateResponse>();

        payload.Should().NotBeNull();
        payload!.Template.Should().NotBeNull();

        payload.Template!.Key.Should().Be("skill-shadow-bite");
        payload.Template.Name.Should().Be("Morsure d’Ombre");
        payload.Template.Status.Should().Be("Active");
        payload.Template.Element.Should().Be("Shadow");
        payload.Template.EffectType.Should().Be("Damage");
        payload.Template.TargetType.Should().Be("SingleEnemy");
    }

    [Fact]
    public async Task GetSkillByKey_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skills/unknown-skill");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSkillByKey_ShouldReturnBadRequest_WhenKeyIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skills/%20%20%20");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Skill template key is required.");
    }

    private sealed record ListSkillTemplatesResponse(
        IReadOnlyCollection<SkillTemplateDto> Templates);

    private sealed record GetSkillTemplateResponse(
        SkillTemplateDto? Template);

    private sealed record SkillTemplateDto(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string Element,
        string EffectType,
        string TargetType,
        int ManaCost,
        int ChargeCost,
        int BasePower,
        int HealPower);
}