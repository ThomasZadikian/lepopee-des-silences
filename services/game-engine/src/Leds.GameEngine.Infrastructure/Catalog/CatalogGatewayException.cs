namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class CatalogGatewayException : Exception
{
    public CatalogGatewayException(string message)
        : base(message)
    {
    }

    public CatalogGatewayException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}