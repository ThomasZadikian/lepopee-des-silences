using System.Net.Http.Json;

namespace Leds.GameEngine.Infrastructure.Outbox;

public sealed class HttpPlayerProjectionClient : IPlayerProjectionClient
{
    private readonly HttpClient _httpClient;

    public HttpPlayerProjectionClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendRunOutcomeAsync(RunOutcomeProjectionEnvelope envelope, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/v2/internal/projections/run-outcomes",
            envelope,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}