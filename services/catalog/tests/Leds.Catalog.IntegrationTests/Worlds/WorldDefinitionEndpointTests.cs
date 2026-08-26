using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Leds.Catalog.IntegrationTests.Worlds;

[Collection("CatalogApi")]
public sealed class WorldDefinitionEndpointTests
{
    private readonly HttpClient _client;

    public WorldDefinitionEndpointTests(CatalogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListWorldDefinitions_ShouldReturnPalaisWithItsEntryRoom()
    {
        var response = await _client.GetAsync("/api/v2/catalog/worlds");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<ListWorldDefinitionsResponse>();

        payload.Should().NotBeNull();
        var palais = payload!.Definitions.Should().ContainSingle(w => w.Key == "palais").Subject;
        palais.EntryRoomKey.Should().Be("room.halldentree");
        palais.Status.Should().Be("Active");
    }

    private sealed record ListWorldDefinitionsResponse(IReadOnlyCollection<WorldDefinitionResponseDto> Definitions);

    private sealed record WorldDefinitionResponseDto(
        Guid Id, string Key, string DisplayName, string EntryRoomKey, string Version, string Status);
}
