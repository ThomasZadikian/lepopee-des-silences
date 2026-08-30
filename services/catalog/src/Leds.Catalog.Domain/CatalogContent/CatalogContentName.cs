using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.CatalogContent;

public readonly record struct CatalogContentName
{
    public CatalogContentName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Catalog content name is required.");
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }

    public static CatalogContentName From(string value)
    {
        return new CatalogContentName(value);
    }
}