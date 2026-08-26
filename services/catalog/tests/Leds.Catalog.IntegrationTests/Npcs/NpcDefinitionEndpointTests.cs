using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Leds.Catalog.IntegrationTests.Npcs;

[Collection("CatalogApi")]
public sealed class NpcDefinitionEndpointTests
{
    private readonly HttpClient _client;

    public NpcDefinitionEndpointTests(CatalogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListNpcDefinitions_ShouldReturnActiveDefinitions()
    {
        var response = await _client.GetAsync("/api/v2/catalog/npc-definitions");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<ListNpcDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().OnlyContain(definition => definition.Status == "Active");
    }

    [Fact]
    public async Task ListNpcDefinitions_ShouldExposeHitomiBoundToRoom08()
    {
        var response = await _client.GetAsync("/api/v2/catalog/npc-definitions");
        var payload = await response.Content.ReadFromJsonAsync<ListNpcDefinitionsResponse>();

        var hitomi = payload!.Definitions.Should().ContainSingle(d => d.Key == "npc.hitomi").Subject;

        hitomi.BoundRoomKeys.Should().BeEquivalentTo("room.room08");
    }

    [Fact]
    public async Task ListNpcDefinitions_ShouldExposeGenericNpcsWithNoBoundRoomKeys()
    {
        var response = await _client.GetAsync("/api/v2/catalog/npc-definitions");
        var payload = await response.Content.ReadFromJsonAsync<ListNpcDefinitionsResponse>();

        var majordome = payload!.Definitions.Should().ContainSingle(d => d.Key == "npc.majordome").Subject;

        majordome.BoundRoomKeys.Should().BeEmpty();
    }

    private sealed record ListNpcDefinitionsResponse(IReadOnlyCollection<NpcDefinitionResponseDto> Definitions);

    private sealed record NpcDefinitionResponseDto(
        Guid Id, string Key, string Name, string Description, string Version, string Status,
        IReadOnlyCollection<string> Tags,
        IReadOnlyCollection<string> CompatibleRoomTypes,
        IReadOnlyCollection<string> CompatiblePalaceRoomStates,
        IReadOnlyCollection<string> CompatibleRoomClimates,
        int? MinDepth, int? MaxDepth,
        string EmotionalAffinity, bool IsRecurring,
        object? Persona, object? DialogueGraph,
        IReadOnlyCollection<object> Wounds,
        IReadOnlyCollection<string> EncounterKeys,
        IReadOnlyCollection<string> BoundRoomKeys,
        IReadOnlyCollection<object> Offerings);
}
