using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Abstractions;

public abstract class CatalogContentBase : ICatalogContent
{
    protected CatalogContentBase(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status)
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

    public CatalogContentVersion Version { get; private set; }

    public CatalogContentStatus Status { get; private set; }

    public bool IsActive => Status == CatalogContentStatus.Active;

    public bool IsDraft => Status == CatalogContentStatus.Draft;

    public bool IsDeprecated => Status == CatalogContentStatus.Deprecated;

    public bool IsDisabled => Status == CatalogContentStatus.Disabled;

    public void Rename(CatalogContentName name)
    {
        Name = name;
    }

    public void ChangeDescription(CatalogContentDescription description)
    {
        Description = description;
    }

    public void ChangeVersion(CatalogContentVersion version)
    {
        Version = version;
    }

    public void Activate()
    {
        if (Status == CatalogContentStatus.Disabled)
        {
            throw new DomainException("Disabled catalog content cannot be activated.");
        }

        Status = CatalogContentStatus.Active;
    }

    public void Deprecate()
    {
        if (Status == CatalogContentStatus.Draft)
        {
            throw new DomainException("Draft catalog content cannot be deprecated.");
        }

        if (Status == CatalogContentStatus.Disabled)
        {
            throw new DomainException("Disabled catalog content cannot be deprecated.");
        }

        Status = CatalogContentStatus.Deprecated;
    }

    public void Disable()
    {
        Status = CatalogContentStatus.Disabled;
    }
}