using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.PalaceLaws.Ports;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Leds.GameEngine.IntegrationTests.Runs;

[Collection("GameEngineApi")]
public sealed class GetRunByIdEndpointTests : RunIntegrationTestBase
{
    private readonly GameEngineApiFactory _factory;
    private readonly HttpClient _client;

    public GetRunByIdEndpointTests(GameEngineApiFactory factory)
        : base(factory.CreateClient())
    {
        _factory = factory;
        _client = Client;
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

        payload.Run.CurrentRoom.TotalNodeCount.Should().BeGreaterThan(0,
            because: "authored rooms may legitimately contain more nodes than procedural rooms");
        allNodes.Should().NotBeEmpty();
        allNodes.Count().Should().BeLessThanOrEqualTo(payload.Run.CurrentRoom.TotalNodeCount,
            because: "the room DTO applies fog of war");
        allNodes.Should().OnlyContain(node => node.State == "Available");
        payload.Run.CurrentRoom.BossPreview.Should().BeNull();
        payload.Run.CurrentRoom.Grid.Should().NotBeNull();
        payload.Run.PalaceIndicators.Should().OnlyContain(indicator =>
            !string.IsNullOrWhiteSpace(indicator.Key)
            && !string.IsNullOrWhiteSpace(indicator.Label)
            && !string.IsNullOrWhiteSpace(indicator.Source));
    }

    [Fact]
    public async Task GetOpenRunForPlayer_ShouldRecoverAnActiveRunWithoutItsId()
    {
        var startRunResponse = await StartRunAsync();

        var response = await _client.GetAsync(
            $"/api/v2/runs/open?playerId={startRunResponse.Run.PlayerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content
            .ReadFromJsonAsync<Leds.GameEngine.Application.Runs.GetOpenRunForPlayer.GetOpenRunForPlayerResponse>();
        payload.Should().NotBeNull();
        payload!.Run.Should().NotBeNull();
        payload.Run!.Id.Should().Be(startRunResponse.Run.Id);
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
                "Le Palais observe la travers�e.",
                "high",
                sourceDecisionId));
        }

        var response = await _client.GetAsync($"/api/v2/runs/{startRunResponse.Run.Id}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<GetRunByIdResponse>();
        payload.Should().NotBeNull();
        payload!.Run.PalaceIndicators.Should().ContainSingle(indicator =>
            indicator.Key == "palace.whispers");

        var indicator = payload.Run.PalaceIndicators!.Single(candidate =>
            candidate.Key == "palace.whispers");
        indicator.Key.Should().Be("palace.whispers");
        indicator.Label.Should().Be("Murmures du Palais");
        indicator.Description.Should().Be("Le Palais observe la travers�e.");
        indicator.Level.Should().Be("high");
        indicator.Source.Should().Be("run");

        using var document = JsonDocument.Parse(body);
        var indicatorJson = document.RootElement
            .GetProperty("run")
            .GetProperty("palaceIndicators")
            .EnumerateArray()
            .Single(candidate => candidate.GetProperty("key").GetString() == "palace.whispers");

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
    public async Task GetRunById_ShouldReturnProjectedPublicPalaceIndicators_FromRunState()
    {
        var startRunResponse = await StartRunAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRunRepository>();
            var run = await repository.GetByIdAsync(
                new RunId(startRunResponse.Run.Id),
                CancellationToken.None);

            run.Should().NotBeNull();
            run!.ActivatePalaceLaw(PalaceLaw.Create(
                "law-rain-v1",
                "Loi de Pluie",
                "v1",
                [PalaceLawDomain.Events],
                [PalaceLawEffect.Create(RunModifierType.RoomClimate, 2, RunModifierDuration.UntilRoomEnds)]));
            run.ApplyCurse(ActiveCurse.Create(
                "curse-test-v1",
                "Pacte brulant",
                "Le prochain combat portera cette marque.",
                0.10,
                DateTime.UtcNow));

            await repository.UpdateAsync(run, CancellationToken.None);
        }

        var response = await _client.GetAsync($"/api/v2/runs/{startRunResponse.Run.Id}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var payload = await response.Content.ReadFromJsonAsync<GetRunByIdResponse>();
        payload.Should().NotBeNull();
        payload!.Run.PalaceIndicators.Should().Contain(indicator =>
            indicator.Key == "law:law-rain-v1" &&
            indicator.Category == "law" &&
            indicator.Source == "law");
        payload.Run.PalaceIndicators.Should().Contain(indicator =>
            indicator.Key == "curse:curse-test-v1" &&
            indicator.Category == "curse" &&
            indicator.Source == "curse");
        payload.Run.PalaceIndicators.Should().Contain(indicator =>
            indicator.Key == "climate:rain" &&
            indicator.Category == "climate" &&
            indicator.Source == "room");

        using var document = JsonDocument.Parse(body);
        var indicatorPropertyNames = document.RootElement
            .GetProperty("run")
            .GetProperty("palaceIndicators")
            .EnumerateArray()
            .SelectMany(indicator => indicator.EnumerateObject())
            .Select(property => property.Name)
            .ToArray();

        indicatorPropertyNames.Should().NotContain([
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
    public async Task GetRunById_ShouldReturnPartySnapshot_AfterNodeSelection()
    {
        var startRunResponse = await StartRunAsync();
        var runId = startRunResponse.Run.Id;

        var firstNode = FirstConfirmableNode(startRunResponse.Run.CurrentRoom);
        await MovePartyAndEnterNodeAsync(runId, firstNode);

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

}
