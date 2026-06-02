using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.Catalog.IntegrationTests.EventTemplates;

public sealed class EventTemplateEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EventTemplateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListEventTemplates_ShouldReturnActiveTemplates()
    {
        var response = await _client.GetAsync("/api/v2/catalog/event-templates");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<ListEventTemplatesResponse>();

        payload.Should().NotBeNull();
        payload!.Templates.Should().NotBeEmpty();
        payload.Templates.Should().OnlyContain(template =>
            template.Status == "Active");

        payload.Templates.Select(template => template.Key)
            .Should()
            .Contain("event-memory-threshold-v1");
    }

    [Fact]
    public async Task GetEventTemplateByKey_ShouldReturnTemplate_WhenKeyExists()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/event-templates/event-memory-threshold-v1");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content
            .ReadFromJsonAsync<GetEventTemplateResponse>();

        payload.Should().NotBeNull();
        payload!.Template.Should().NotBeNull();

        payload.Template!.Key.Should().Be("event-memory-threshold-v1");
        payload.Template.Name.Should().Be("Mémoire du seuil");
        payload.Template.Status.Should().Be("Active");
        payload.Template.Type.Should().Be("Memory");
        payload.Template.DefaultOutcomeKind.Should().Be("TomePageUnlocked");
        payload.Template.NarrativeTags.Should().Contain("memory");
        payload.Template.NarrativeTags.Should().Contain("threshold");
        payload.Template.NarrativeTags.Should().Contain("elise");
    }

    [Fact]
    public async Task GetEventTemplateByKey_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/event-templates/unknown-event");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEventTemplateByKey_ShouldReturnBadRequest_WhenKeyIsWhitespace()
    {
        var response = await _client.GetAsync(
            "/api/v2/catalog/event-templates/%20%20%20");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Event template key is required.");
    }

    private sealed record ListEventTemplatesResponse(
        IReadOnlyCollection<EventTemplateDto> Templates);

    private sealed record GetEventTemplateResponse(
        EventTemplateDto? Template);

    private sealed record EventTemplateDto(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string Type,
        string DefaultOutcomeKind,
        int MinRiskLevel,
        int MaxRiskLevel,
        bool RequiresPlayerChoice,
        IReadOnlyCollection<string> NarrativeTags);
}