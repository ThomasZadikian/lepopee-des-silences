using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Numerics;

namespace Leds.Catalog.IntegrationTests.PalaceLaws;

public sealed class PalaceLawDefinitionEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PalaceLawDefinitionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListPalaceLaws_ShouldReturnActiveDefinitions()
    {
        var response = await _client.GetAsync("/api/v2/catalog/palace-laws");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListPalaceLawDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().OnlyContain(definition =>
            definition.Status == "Active");

        payload.Definitions.Select(definition => definition.Key)
            .Should()
            .Contain("law-silence-v1");
    }

    [Fact]
    public async Task GetPalaceLawByKey_ShouldReturnDefinition_WhenKeyExists()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/palace-laws/law-silence-v1");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetPalaceLawDefinitionResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();

        payload.Definition!.Key.Should().Be("law-silence-v1");
        payload.Definition.Name.Should().Be("Loi du Silence");
        payload.Definition.Status.Should().Be("Active");
        payload.Definition.Visibility.Should().Be("PartiallyVisible");
        payload.Definition.ImpactDomains.Should().Contain("Generation");
        payload.Definition.ImpactDomains.Should().Contain("Narrative");
    }

    [Fact]
    public async Task GetPalaceLawByKey_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/palace-laws/unknown-law");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPalaceLawByKey_ShouldReturnBadRequest_WhenKeyIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/palace-laws/%20%20%20");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Palace law definition key is required.");
    }

    private sealed record ListPalaceLawDefinitionsResponse(
        IReadOnlyCollection<PalaceLawDefinitionDto> Definitions);

    private sealed record GetPalaceLawDefinitionResponse(
        PalaceLawDefinitionDto? Definition);

    private sealed record PalaceLawDefinitionDto(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string Visibility,
        int Priority,
        IReadOnlyCollection<string> ImpactDomains);
}