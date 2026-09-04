namespace Leds.Player.Infrastructure.Catalog;

public sealed class CatalogGatewayOptions
{
    public const string SectionName = "CatalogGateway";
    public string BaseUrl { get; init; } = "http://catalog:8080";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
