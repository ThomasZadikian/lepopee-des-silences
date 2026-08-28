using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Leds.Catalog.IntegrationTests.NpcReputationAffinities;

[Collection("CatalogApi")]
public sealed class NpcReputationAffinityEndpointTests
{
    private readonly HttpClient _client;

    public NpcReputationAffinityEndpointTests(CatalogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListNpcReputationAffinities_ShouldReturnOk()
    {
        // No editorial pairs are seeded yet (tissage inter-PNJ, point ouvert de la SFD) —
        // this only asserts the endpoint responds correctly with whatever the table
        // currently holds, since the game-engine falls back to a default weight of 0
        // (no bleed-over) for any unlisted pair.
        var response = await _client.GetAsync("/api/v2/catalog/npc-reputation-affinities");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<ListNpcReputationAffinitiesResponse>();
        payload.Should().NotBeNull();
        payload!.Affinities.Should().NotBeNull();
    }

    private sealed record ListNpcReputationAffinitiesResponse(IReadOnlyCollection<NpcReputationAffinityResponseDto> Affinities);

    private sealed record NpcReputationAffinityResponseDto(Guid Id, string NpcKeyFrom, string NpcKeyTo, decimal Weight);
}
