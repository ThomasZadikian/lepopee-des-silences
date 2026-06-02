using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.Catalog.IntegrationTests.Health;

public sealed class CatalogHealthEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CatalogHealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthyStatus()
    {
        var response = await _client.GetAsync("/api/v2/catalog/health");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<CatalogHealthResponse>();

        payload.Should().NotBeNull();
        payload!.Service.Should().Be("catalog");
        payload.Status.Should().Be("Healthy");
        payload.Version.Should().Be("alpha-0.0.4");
    }

    private sealed record CatalogHealthResponse(
        string Service,
        string Status,
        string Version);
}