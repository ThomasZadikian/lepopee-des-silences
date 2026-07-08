using FluentAssertions;
using Leds.Catalog.Application.Skills.Definitions.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Leds.Catalog.IntegrationTests.Skills.Definitions;

public sealed class SkillDefinitionEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SkillDefinitionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListSkillDefinitions_ShouldReturn200()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListSkillDefinitions_ShouldReturnSeededSkills()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions");

        var payload = await response.Content
            .ReadFromJsonAsync<ListSkillDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().OnlyContain(d => d.Status == "Active");
        payload.Definitions.Select(d => d.Key)
            .Should()
            .Contain("skill.basic.strike")
            .And.Contain("skill.basic.guard")
            .And.Contain("skill.basic.weaken")
            .And.Contain("skill.basic.disrupt")
            .And.Contain("skill.basic.focus");
    }

    [Fact]
    public async Task GetSkillDefinitionByKey_ShouldReturnExpectedSkill()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions/skill.basic.strike");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK, because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetSkillDefinitionResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();

        payload.Definition!.Key.Should().Be("skill.basic.strike");
        payload.Definition.Name.Should().Be("Frappe");
        payload.Definition.Status.Should().Be("Active");
        payload.Definition.SkillType.Should().Be("Damage");
        payload.Definition.TargetingType.Should().Be("SingleEnemy");
        payload.Definition.EffectType.Should().Be("Damage");
        payload.Definition.ManaCost.Should().Be(5);
        payload.Definition.ChargeCost.Should().Be(0);
        payload.Definition.BasePower.Should().Be(10);
    }

    [Fact]
    public async Task GetSkillDefinitionByKey_ShouldExposeMultipleSimultaneousEffects_ForConstructionPerpetuelle()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions/canon.skill.construction-perpetuelle");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK, because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetSkillDefinitionResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();
        payload.Definition!.Effects.Should().HaveCount(2);
        payload.Definition.Effects.Should().Contain(e =>
            e.Kind == "HealOverTime" && e.MagnitudeIsPercentOfMax);
        payload.Definition.Effects.Should().Contain(e =>
            e.Kind == "GuardOverTime" && !e.MagnitudeIsPercentOfMax);
    }

    [Fact]
    public async Task GetSkillDefinitionByKey_ShouldReturn404_WhenUnknown()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions/unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSkillDefinitionByKey_ShouldReturn400_WhenKeyIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions/%20%20%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSkillDefinitionsByType_ShouldReturnDamageSkills()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions/type/Damage");

        var payload = await response.Content
            .ReadFromJsonAsync<ListSkillDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().AllSatisfy(d =>
            d.SkillType.Should().Be("Damage"));
    }

    [Fact]
    public async Task GetSkillDefinitionsByType_ShouldReturnDebuffSkills()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions/type/Debuff");

        var payload = await response.Content
            .ReadFromJsonAsync<ListSkillDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Select(d => d.Key)
            .Should()
            .Contain("skill.basic.weaken")
            .And.Contain("skill.basic.disrupt");
    }

    [Fact]
    public async Task GetSkillDefinitionsByType_ShouldReturnEmpty_WhenUnknownType()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions/type/Unknown");

        var payload = await response.Content
            .ReadFromJsonAsync<ListSkillDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkillDefinitionsByKeys_ShouldReturnMatching()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v2/catalog/skill-definitions/batch/by-keys",
            new { keys = new[] { "skill.basic.strike", "skill.basic.guard" } });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK, because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListSkillDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Select(d => d.Key)
            .Should()
            .Contain("skill.basic.strike")
            .And.Contain("skill.basic.guard");
    }

    [Fact]
    public async Task GetSkillDefinitionsByKeys_ShouldReturn400_WhenKeysMissing()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v2/catalog/skill-definitions/batch/by-keys",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListActive_ShouldOnlyContainActiveSkills()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/skill-definitions");

        var payload = await response.Content
            .ReadFromJsonAsync<ListSkillDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().OnlyContain(d => d.Status == "Active");
    }

    private sealed record ListSkillDefinitionsResponse(
        IReadOnlyCollection<SkillDefinitionDto> Definitions);

    private sealed record GetSkillDefinitionResponse(
        SkillDefinitionDto? Definition);
}
