using FluentAssertions;
using Leds.GameEngine.Application.PalaceLaws.Ports;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Domain.PalaceLaws;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class GetRunByIdEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetRunByIdEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRunById_ShouldReturnRun_WhenRunExists()
    {
        var startRunResponse = await StartRunAsync();

        var response = await _client.GetAsync($"/api/v2/runs/{startRunResponse.Run.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GetRunByIdResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().Be(startRunResponse.Run.Id);
        payload.Run.PlayerId.Should().Be(startRunResponse.Run.PlayerId);
        payload.Run.Status.Should().Be("Active");
        var allNodes = payload!.Run.CurrentRoom.Nodes.ToArray();

        payload.Run.CurrentRoom.TotalNodeCount.Should().BeInRange(6, 30);
        allNodes.Should().HaveCount(payload.Run.CurrentRoom.TotalNodeCount);

        payload.Run.CurrentRoom.AvailableNodes.Should().HaveCountGreaterThanOrEqualTo(1);
        payload.Run.CurrentRoom.AvailableNodes.Should().HaveCountLessThanOrEqualTo(4);
        payload.Run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.State == "Available");
        payload.Run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.Row == 0);

        allNodes.Where(node => node.Row > 0)
            .Should()
            .OnlyContain(node => node.State == "Planned");

        allNodes.Should().ContainSingle(node => node.IsBoss);

        payload.Run.CurrentRoom.BossPreview.Should().NotBeNull();
        payload.Run.CurrentRoom.BossPreview.Name.Should().NotBeNullOrWhiteSpace();
        payload.Run.PalaceIndicators.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRunById_ShouldReturnPublicPalaceIndicators_WhenIndicatorsExist()
    {
        var startRunResponse = await StartRunAsync();
        var sourceDecisionId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPalaceIndicatorRepository>();
            await repository.AddAsync(PalaceIndicator.Create(
                startRunResponse.Run.Id,
                "palace.whispers",
                "Murmures du Palais",
                "Le Palais observe la traversée.",
                "high",
                sourceDecisionId));
        }

        var response = await _client.GetAsync($"/api/v2/runs/{startRunResponse.Run.Id}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<GetRunByIdResponse>();
        payload.Should().NotBeNull();
        payload!.Run.PalaceIndicators.Should().ContainSingle();

        var indicator = payload.Run.PalaceIndicators!.Single();
        indicator.Key.Should().Be("palace.whispers");
        indicator.Label.Should().Be("Murmures du Palais");
        indicator.Description.Should().Be("Le Palais observe la traversée.");
        indicator.Level.Should().Be("high");
        indicator.Source.Should().Be("run");

        using var document = JsonDocument.Parse(body);
        var indicatorJson = document.RootElement
            .GetProperty("run")
            .GetProperty("palaceIndicators")
            .EnumerateArray()
            .Single();

        var propertyNames = indicatorJson
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        propertyNames.Should().NotContain([
            "weight",
            "probability",
            "coefficient",
            "matrix",
            "markov",
            "rawScore",
            "adaptiveScore",
            "sourceDecisionId",
            "createdAtUtc",
            "expiresAtUtc"
        ]);
    }

    [Fact]
    public async Task GetRunById_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        var unknownRunId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v2/runs/{unknownRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Resource not found.");
        body.Should().Contain($"Run with id '{unknownRunId}' was not found.");
    }

    [Fact]
    public async Task GetRunById_ShouldReturnPartySnapshot_AfterRunStart()
    {
        var startRunResponse = await StartRunAsync();

        var response = await _client.GetAsync($"/api/v2/runs/{startRunResponse.Run.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GetRunByIdResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Party.Should().NotBeNull("party snapshot must be present after run creation");
        payload.Run.Party!.Members.Should().NotBeEmpty();

        var member = payload.Run.Party.Members.First();
        member.DisplayName.Should().NotBeNullOrWhiteSpace();
        member.MaxVitality.Should().BeGreaterThan(0);
        member.CurrentVitality.Should().BeGreaterThan(0);
        member.CurrentVitality.Should().BeLessThanOrEqualTo(member.MaxVitality);
        member.IsActive.Should().BeTrue();
        member.Skills.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRunById_ShouldReturnPartySnapshot_AfterNodeChoice()
    {
        var startRunResponse = await StartRunAsync();
        var runId = startRunResponse.Run.Id;

        var firstNode = startRunResponse.Run.CurrentRoom.AvailableNodes.First();

        var chooseResponse = await _client.PostAsJsonAsync(
            $"/api/v2/runs/{runId}/nodes/{firstNode.Id}/choose",
            new { });

        chooseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync($"/api/v2/runs/{runId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GetRunByIdResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Party.Should().NotBeNull(
            "party snapshot must remain available after node choice, independent of combat state");
    }

    [Fact]
    public async Task StartRun_ShouldReturnPartySnapshotInInitialResponse()
    {
        var startRunResponse = await StartRunAsync();

        startRunResponse.Run.Party.Should().NotBeNull(
            "party snapshot must be available immediately after POST /api/v2/runs");
        startRunResponse.Run.Party!.Members.Should().NotBeEmpty();
    }

    private async Task<StartRunResponse> StartRunAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v2/runs",
            new
            {
                PlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111")
            });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();

        return payload!;
    }
}
