namespace Leds.Catalog.Domain.Common;

public readonly record struct CatalogContentId(Guid Value)
{
    public static CatalogContentId New()
    {
        return new CatalogContentId(Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}