namespace Leds.Catalog.Domain.CatalogContent;

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