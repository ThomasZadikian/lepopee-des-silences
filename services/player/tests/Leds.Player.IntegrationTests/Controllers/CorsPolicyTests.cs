using System.Net;
using FluentAssertions;

namespace Leds.Player.IntegrationTests.Controllers;

[Collection("PlayerApi")]
public sealed class CorsPolicyTests
{
    private readonly HttpClient _client;

    public CorsPolicyTests(PlayerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LoginPreflight_ShouldAllowGameClientOriginAndCredentials()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/v2/account/login");
        request.Headers.Add("Origin", PlayerApiFactory.GameClientOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle(PlayerApiFactory.GameClientOrigin);
        response.Headers.GetValues("Access-Control-Allow-Credentials")
            .Should().ContainSingle("true");
        response.Headers.GetValues("Access-Control-Allow-Methods")
            .Should().Contain(value => value.Contains("POST", StringComparison.OrdinalIgnoreCase));
        response.Headers.GetValues("Access-Control-Allow-Headers")
            .Should().Contain(value => value.Contains("content-type", StringComparison.OrdinalIgnoreCase));
    }
}
