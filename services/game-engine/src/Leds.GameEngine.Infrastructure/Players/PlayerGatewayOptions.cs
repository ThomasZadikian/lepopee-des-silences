namespace Leds.GameEngine.Infrastructure.Players;

public sealed class PlayerGatewayOptions
{
    public const string SectionName = "PlayerGateway";
    public string Mode { get; init; } = "InMemory";
    public string BaseUrl { get; init; } = "http://localhost:5189";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
}
