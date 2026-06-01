namespace Leds.Catalog.Domain.Common;

public abstract class CatalogContentBase
{
    protected CatalogContentBase(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogVersion version,
        CatalogItemStatus status)
    {
        Id = id;
        Key = key;
        Name = name;
        Description = description;
        Version = version;
        Status = status;
    }

    public CatalogContentId Id { get; }

    public CatalogContentKey Key { get; private set; }

    public CatalogContentName Name { get; private set; }

    public CatalogContentDescription Description { get; private set; }

    public CatalogVersion Version { get; private set; }

    public CatalogItemStatus Status { get; private set; }

    public bool IsActive => Status == CatalogItemStatus.Active;

    public bool IsDraft => Status == CatalogItemStatus.Draft;

    public bool IsDeprecated => Status == CatalogItemStatus.Deprecated;

    public bool IsDisabled => Status == CatalogItemStatus.Disabled;

    public void Rename(CatalogContentName name)
    {
        Name = name;
    }

    public void ChangeDescription(CatalogContentDescription description)
    {
        Description = description;
    }

    public void ChangeVersion(CatalogVersion version)
    {
        Version = version;
    }

    public void Activate()
    {
        if (Status == CatalogItemStatus.Disabled)
        {
            throw new DomainException("Disabled catalog content cannot be activated.");
        }

        Status = CatalogItemStatus.Active;
    }

    public void Deprecate()
    {
        if (Status == CatalogItemStatus.Draft)
        {
            throw new DomainException("Draft catalog content cannot be deprecated.");
        }

        if (Status == CatalogItemStatus.Disabled)
        {
            throw new DomainException("Disabled catalog content cannot be deprecated.");
        }

        Status = CatalogItemStatus.Deprecated;
    }

    public void Disable()
    {
        Status = CatalogItemStatus.Disabled;
    }
}