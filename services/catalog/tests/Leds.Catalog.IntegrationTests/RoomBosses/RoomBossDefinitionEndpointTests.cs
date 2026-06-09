using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Leds.Catalog.IntegrationTests.RoomBosses;

public sealed class RoomBossDefinitionEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RoomBossDefinitionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListRoomBossDefinitions_ShouldReturnActiveDefinitions()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/room-boss-definitions");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListRoomBossDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().OnlyContain(definition =>
            definition.Status == "Active");

        payload.Definitions.Select(definition => definition.Key)
            .Should()
            .Contain("boss.threshold.warden");
    }

    [Fact]
    public async Task GetRoomBossDefinitionByKey_ShouldReturnDefinition_WhenKeyExists()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/room-boss-definitions/boss.threshold.warden");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetRoomBossDefinitionResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();

        payload.Definition!.Key.Should().Be("boss.threshold.warden");
        payload.Definition.Name.Should().Be("Warden of the Threshold");
        payload.Definition.Status.Should().Be("Active");
        payload.Definition.RoomType.Should().Be("Threshold");
        payload.Definition.BaseDifficulty.Should().Be(1);
        payload.Definition.Tags.Should().Contain("sentinel");
    }

    [Fact]
    public async Task GetRoomBossDefinitionByKey_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/room-boss-definitions/unknown-boss");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRoomBossDefinitionByKey_ShouldReturnBadRequest_WhenKeyIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/room-boss-definitions/%20%20%20");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Room boss definition key is required.");
    }

    [Fact]
    public async Task GetRoomBossDefinitionByRoomType_ShouldReturnDefinition_WhenRoomTypeExists()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/room-boss-definitions/room-type/Threshold");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetRoomBossDefinitionByRoomTypeResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();

        payload.Definition!.RoomType.Should().Be("Threshold");
        payload.Definition.BaseDifficulty.Should().Be(1);
    }

    [Fact]
    public async Task GetRoomBossDefinitionByRoomType_ShouldReturnNotFound_WhenRoomTypeDoesNotExist()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/room-boss-definitions/room-type/Unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRoomBossDefinitionByRoomType_ShouldReturnBadRequest_WhenRoomTypeIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/room-boss-definitions/room-type/%20%20%20");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Room type is required.");
    }

    private sealed record ListRoomBossDefinitionsResponse(
        IReadOnlyCollection<RoomBossDefinitionDto> Definitions);

    private sealed record GetRoomBossDefinitionResponse(
        RoomBossDefinitionDto? Definition);

    private sealed record GetRoomBossDefinitionByRoomTypeResponse(
        RoomBossDefinitionDto? Definition);

    private sealed record RoomBossDefinitionDto(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string RoomType,
        int BaseDifficulty,
        IReadOnlyCollection<string> Tags);
}
