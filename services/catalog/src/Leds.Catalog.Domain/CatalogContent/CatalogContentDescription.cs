namespace Leds.Catalog.Domain.CatalogContent;

public readonly record struct CatalogContentDescription
{
    public CatalogContentDescription(string value)
    {
        Value = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public override string ToString()
    {
        return Value;
    }

    public static CatalogContentDescription From(string? value)
    {
        return new CatalogContentDescription(value ?? string.Empty);
    }
}