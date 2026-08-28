using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Leds.Catalog.IntegrationTests.RoomThemeAffinities;

[Collection("CatalogApi")]
public sealed class RoomThemeAffinityEndpointTests
{
    private readonly HttpClient _client;

    public RoomThemeAffinityEndpointTests(CatalogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListRoomThemeAffinities_ShouldReturnOk()
    {
        // No editorial overrides are seeded yet (SFD Annexe B, point ouvert) — this only
        // asserts the endpoint responds correctly with whatever the table currently holds,
        // since the game-engine falls back to a default weight for any unlisted pair.
        var response = await _client.GetAsync("/api/v2/catalog/room-theme-affinities");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<ListRoomThemeAffinitiesResponse>();
        payload.Should().NotBeNull();
        payload!.Affinities.Should().NotBeNull();
    }

    private sealed record ListRoomThemeAffinitiesResponse(IReadOnlyCollection<RoomThemeAffinityResponseDto> Affinities);

    private sealed record RoomThemeAffinityResponseDto(Guid Id, string ThemeFrom, string ThemeTo, decimal Weight);
}
