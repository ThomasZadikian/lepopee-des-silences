using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.Catalog.IntegrationTests.Items;

public sealed class ItemTemplateEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ItemTemplateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListItems_ShouldReturnActiveItemTemplates()
    {
        var response = await _client.GetAsync("/api/v2/catalog/items");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListItemTemplatesResponse>();

        payload.Should().NotBeNull();
        payload!.Templates.Should().NotBeEmpty();
        payload.Templates.Should().OnlyContain(template =>
            template.Status == "Active");

        payload.Templates.Select(template => template.Key)
            .Should()
            .Contain("item-memory-potion");
    }

    [Fact]
    public async Task GetItemByKey_ShouldReturnItemTemplate_WhenKeyExists()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/items/item-memory-potion");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetItemTemplateResponse>();

        payload.Should().NotBeNull();
        payload!.Template.Should().NotBeNull();

        payload.Template!.Key.Should().Be("item-memory-potion");
        payload.Template.Name.Should().Be("Potion de Mémoire");
        payload.Template.Status.Should().Be("Active");
        payload.Template.Category.Should().Be("Consumable");
        payload.Template.Rarity.Should().Be("Common");
        payload.Template.Duration.Should().Be("RunOnly");
    }

    [Fact]
    public async Task GetItemByKey_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/items/unknown-item");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetItemByKey_ShouldReturnBadRequest_WhenKeyIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/items/%20%20%20");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Item template key is required.");
    }

    private sealed record ListItemTemplatesResponse(
        IReadOnlyCollection<ItemTemplateDto> Templates);

    private sealed record GetItemTemplateResponse(
        ItemTemplateDto? Template);

    private sealed record ItemTemplateDto(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string Category,
        string Rarity,
        string Duration,
        int EffectValue,
        int Price);
}