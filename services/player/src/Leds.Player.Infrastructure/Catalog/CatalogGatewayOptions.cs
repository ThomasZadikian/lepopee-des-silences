namespace Leds.Player.Infrastructure.Catalog;

public sealed class CatalogGatewayOptions
{
    public const string SectionName = "CatalogGateway";
    public string BaseUrl { get; init; } = "https://catalog:8443";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
