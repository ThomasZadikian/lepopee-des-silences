using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Leds.GameEngine.IntegrationTests.Middleware;

public sealed class CorrelationIdMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CorrelationIdMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_ShouldCreateCorrelationId_WhenHeaderMissing()
    {
        var response = await _client.GetAsync("/api/v2/runs/nonexistent");

        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        var correlationId = response.Headers.GetValues("X-Correlation-Id").Single();
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_ShouldReuseCorrelationId_WhenHeaderProvided()
    {
        var expected = "test-correlation-001";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/runs/nonexistent");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", expected);

        var response = await _client.SendAsync(request);

        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        response.Headers.GetValues("X-Correlation-Id").Single().Should().Be(expected);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_ShouldReturnCorrelationIdHeader_OnError()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/runs/nonexistent");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", "test-error-correlation");

        var response = await _client.SendAsync(request);

        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        response.Headers.GetValues("X-Correlation-Id").Single().Should().Be("test-error-correlation");
    }
}
