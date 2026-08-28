using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Numerics;

namespace Leds.Catalog.IntegrationTests.PalaceLaws;

[Collection("CatalogApi")]
public sealed class PalaceLawDefinitionEndpointTests
{
    private readonly HttpClient _client;

    public PalaceLawDefinitionEndpointTests(CatalogApiFactory factory)
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
            .Contain("law.silence-du");
    }

    [Fact]
    public async Task GetPalaceLawByKey_ShouldReturnDefinition_WhenKeyExists()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/palace-laws/law.silence-du");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetPalaceLawDefinitionResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();

        payload.Definition!.Key.Should().Be("law.silence-du");
        payload.Definition.Name.Should().Be("Loi du Silence Dû");
        payload.Definition.Status.Should().Be("Active");
        payload.Definition.Visibility.Should().Be("Visible");
        payload.Definition.ImpactDomains.Should().Contain("Combat");
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
