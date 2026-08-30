using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.CatalogContent;

public readonly record struct CatalogContentKey
{
    public CatalogContentKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Catalog content key is required.");
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }

    public static CatalogContentKey From(string value)
    {
        return new CatalogContentKey(value);
    }
}