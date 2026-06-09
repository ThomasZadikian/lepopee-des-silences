namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class CatalogGatewayOptions
{
    public const string SectionName = "CatalogGateway";

    public string Mode { get; init; } = "InMemory";

    public string BaseUrl { get; init; } = "http://localhost:5193";

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
}
