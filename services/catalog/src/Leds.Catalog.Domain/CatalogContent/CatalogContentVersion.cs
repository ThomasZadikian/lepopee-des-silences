using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.CatalogContent;

public readonly record struct CatalogContentVersion
{
    public CatalogContentVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Catalog version is required.");
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }

    public static CatalogContentVersion From(string value)
    {
        return new CatalogContentVersion(value);
    }
}