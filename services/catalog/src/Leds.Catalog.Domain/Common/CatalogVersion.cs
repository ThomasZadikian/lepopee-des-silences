namespace Leds.Catalog.Domain.Common;

public readonly record struct CatalogVersion
{
    public CatalogVersion(string value)
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

    public static CatalogVersion From(string value)
    {
        return new CatalogVersion(value);
    }
}