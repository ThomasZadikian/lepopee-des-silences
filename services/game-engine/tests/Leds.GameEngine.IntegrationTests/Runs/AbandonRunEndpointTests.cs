using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.GameEngine.Application.Runs.AbandonRun;
using Leds.GameEngine.Application.Runs.StartRun;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Runs;

public sealed class AbandonRunEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AbandonRunEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AbandonRun_ShouldReturnOk_WhenRunIsActive()
    {
        // Arrange
        var startRunResponse = await StartRunAsync();

        // Act
        var response = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/abandon",
            content: null);

        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<AbandonRunResponse>();

        payload.Should().NotBeNull();
        payload!.Run.Id.Should().Be(startRunResponse.Run.Id);
        payload.Run.Status.Should().Be("Abandoned");
    }

    [Fact]
    public async Task AbandonRun_ShouldReturnNotFound_WhenRunDoesNotExist()
    {
        // Arrange
        var unknownRunId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync(
            $"/api/v2/runs/{unknownRunId}/abandon",
            content: null);

        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            because: body);

        body.Should().Contain("Resource not found.");
        body.Should().Contain($"Run with id '{unknownRunId}' was not found.");
    }

    [Fact]
    public async Task AbandonRun_ShouldReturnBadRequest_WhenRunIsAlreadyAbandoned()
    {
        // Arrange
        var startRunResponse = await StartRunAsync();

        var firstResponse = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/abandon",
            content: null);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var secondResponse = await _client.PostAsync(
            $"/api/v2/runs/{startRunResponse.Run.Id}/abandon",
            content: null);

        var body = await secondResponse.Content.ReadAsStringAsync();

        // Assert
        secondResponse.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            because: body);

        body.Should().Contain("Domain rule violated.");
        body.Should().Contain("Run is already closed.");
    }

    private async Task<StartRunResponse> StartRunAsync()
    {
        var playerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var response = await _client.PostAsJsonAsync(
            "/api/v2/runs",
            new { PlayerId = playerId });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            because: body);

        var payload = await response.Content.ReadFromJsonAsync<StartRunResponse>();

        payload.Should().NotBeNull();

        return payload!;
    }
}