using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Leds.Catalog.IntegrationTests.Items;

[Collection("CatalogApi")]
public sealed class ItemDefinitionContractEndpointTests
{
    private readonly HttpClient _client;

    public ItemDefinitionContractEndpointTests(CatalogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListItems_ShouldReturnActiveItemDefinitions()
    {
        var response = await _client.GetAsync("/api/v2/catalog/item-definitions");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListItemDefinitionsResponse>();

        payload.Should().NotBeNull();
        payload!.Definitions.Should().NotBeEmpty();
        payload.Definitions.Should().OnlyContain(definition =>
            definition.Status == "Active");

        payload.Definitions.Select(definition => definition.Key)
            .Should()
            .Contain("canon.item.potion-de-vie");
    }

    [Fact]
    public async Task GetItemByKey_ShouldReturnItemTemplate_WhenKeyExists()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/item-definitions/canon.item.potion-de-vie");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetItemDefinitionResponse>();

        payload.Should().NotBeNull();
        payload!.Definition.Should().NotBeNull();

        payload.Definition!.Key.Should().Be("canon.item.potion-de-vie");
        payload.Definition.DisplayName.Should().Be("Potion de vie");
        payload.Definition.Status.Should().Be("Active");
        payload.Definition.Category.Should().Be("Consumable");
        payload.Definition.Rarity.Should().Be("Common");
        payload.Definition.Lifecycle.Should().Be("RuntimeRunOnly");
    }

    [Fact]
    public async Task GetItemByKey_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/item-definitions/unknown-item");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetItemByKey_ShouldReturnBadRequest_WhenKeyIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/item-definitions/%20%20%20");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Item definition key is required.");
    }

    private sealed record ListItemDefinitionsResponse(
        IReadOnlyCollection<ItemDefinitionDto> Definitions);

    private sealed record GetItemDefinitionResponse(
        ItemDefinitionDto? Definition);

    private sealed record ItemDefinitionDto(
        Guid Id,
        string Key,
        string DisplayName,
        string Description,
        string Version,
        string Status,
        string Category,
        string Rarity,
        string Lifecycle,
        int EffectValue);
}
