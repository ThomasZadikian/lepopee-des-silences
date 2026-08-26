using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Leds.Catalog.IntegrationTests.Rooms;

[Collection("CatalogApi")]
public sealed class RoomDefinitionEndpointTests
{
    private readonly HttpClient _client;

    public RoomDefinitionEndpointTests(CatalogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListRoomDefinitions_ShouldReturnActiveDefinitions()
    {
        var response = await _client.GetAsync("/api/v2/catalog/room-definitions");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<ListRoomDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().OnlyContain(definition => definition.Status == "Active");
    }

    [Fact]
    public async Task ListRoomDefinitions_ShouldExposePalaisEntryRoomWithReachableChildren()
    {
        var response = await _client.GetAsync("/api/v2/catalog/room-definitions");
        var payload = await response.Content.ReadFromJsonAsync<ListRoomDefinitionsResponse>();

        var hallDentree = payload!.Definitions.Should().ContainSingle(d => d.Key == "room.halldentree").Subject;

        hallDentree.WorldKey.Should().Be("palais");
        hallDentree.IsWorldEntryRoom.Should().BeTrue();
    }

    [Fact]
    public async Task ListRoomDefinitions_ShouldResolveStrictChainForFalaise()
    {
        var response = await _client.GetAsync("/api/v2/catalog/room-definitions");
        var payload = await response.Content.ReadFromJsonAsync<ListRoomDefinitionsResponse>();

        var falaise = payload!.Definitions.Should().ContainSingle(d => d.Key == "room.falaise").Subject;

        falaise.TriggersStrictChain.Should().BeTrue();
        falaise.ReachableRoomKeys.Should().BeEquivalentTo("room.enfer1");

        var enfer4 = payload.Definitions.Should().ContainSingle(d => d.Key == "room.enfer4").Subject;
        enfer4.ReachableRoomKeys.Should().BeEmpty();
    }

    private sealed record ListRoomDefinitionsResponse(IReadOnlyCollection<RoomDefinitionResponseDto> Definitions);

    private sealed record RoomDefinitionResponseDto(
        Guid Id, string Key, string DisplayName, string Description, string? NarrativeText,
        string RoomFamily, string RoomRarity, string Theme,
        int? MinDepth, int? MaxDepth, int BaseWeight, string? SelectionGroup,
        string? EnemyPoolKey, string? RewardPoolKey, string? LawPoolKey, string? CursePoolKey,
        string? SpecialMechanicKey, string? BossDefinitionKey,
        bool IsUnique, bool IsCulturalEcho,
        string? WorldKey, bool IsWorldEntryRoom, bool TriggersStrictChain,
        IReadOnlyCollection<string> ReachableRoomKeys,
        string Version, string Status);
}
