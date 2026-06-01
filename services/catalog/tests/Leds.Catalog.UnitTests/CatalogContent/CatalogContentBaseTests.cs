using FluentAssertions;
using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.UnitTests.CatalogContent;

public sealed class CatalogContentBaseTests
{
    [Fact]
    public void Create_ShouldCreateDraftContent_WhenInputIsValid()
    {
        var content = TestCatalogContent.Create(
            "enemy-shadow-wolf",
            "Loup d’Ombre",
            "Une entité née dans un couloir du Palais.",
            "catalog-0.1.0");

        content.Id.Value.Should().NotBeEmpty();
        content.Key.Value.Should().Be("enemy-shadow-wolf");
        content.Name.Value.Should().Be("Loup d’Ombre");
        content.Description.Value.Should().Be("Une entité née dans un couloir du Palais.");
        content.Version.Value.Should().Be("catalog-0.1.0");
        content.Status.Should().Be(CatalogContentStatus.Draft);

        content.IsDraft.Should().BeTrue();
        content.IsActive.Should().BeFalse();
        content.IsDeprecated.Should().BeFalse();
        content.IsDisabled.Should().BeFalse();
    }

    [Fact]
    public void CatalogContentBase_ShouldImplementICatalogContent()
    {
        var content = CreateContent();

        content.Should().BeAssignableTo<ICatalogContent>();
    }

    [Fact]
    public void Rename_ShouldChangeName()
    {
        var content = CreateContent();

        content.Rename(CatalogContentName.From("Nouveau nom"));

        content.Name.Value.Should().Be("Nouveau nom");
    }

    [Fact]
    public void ChangeDescription_ShouldChangeDescription()
    {
        var content = CreateContent();

        content.ChangeDescription(CatalogContentDescription.From("Nouvelle description"));

        content.Description.Value.Should().Be("Nouvelle description");
    }

    [Fact]
    public void ChangeVersion_ShouldChangeVersion()
    {
        var content = CreateContent();

        content.ChangeVersion(CatalogContentVersion.From("catalog-0.2.0"));

        content.Version.Value.Should().Be("catalog-0.2.0");
    }

    [Fact]
    public void Activate_ShouldSetStatusToActive_WhenContentIsDraft()
    {
        var content = CreateContent();

        content.Activate();

        content.Status.Should().Be(CatalogContentStatus.Active);
        content.IsActive.Should().BeTrue();
        content.IsDraft.Should().BeFalse();
    }

    [Fact]
    public void Disable_ShouldSetStatusToDisabled()
    {
        var content = CreateContent();

        content.Disable();

        content.Status.Should().Be(CatalogContentStatus.Disabled);
        content.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldThrowDomainException_WhenContentIsDisabled()
    {
        var content = CreateContent();

        content.Disable();

        var act = content.Activate;

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Disabled catalog content cannot be activated.");
    }

    [Fact]
    public void Deprecate_ShouldSetStatusToDeprecated_WhenContentIsActive()
    {
        var content = CreateContent();

        content.Activate();
        content.Deprecate();

        content.Status.Should().Be(CatalogContentStatus.Deprecated);
        content.IsDeprecated.Should().BeTrue();
        content.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deprecate_ShouldThrowDomainException_WhenContentIsDraft()
    {
        var content = CreateContent();

        var act = content.Deprecate;

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Draft catalog content cannot be deprecated.");
    }

    [Fact]
    public void Deprecate_ShouldThrowDomainException_WhenContentIsDisabled()
    {
        var content = CreateContent();

        content.Disable();

        var act = content.Deprecate;

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Disabled catalog content cannot be deprecated.");
    }

    private static TestCatalogContent CreateContent()
    {
        return TestCatalogContent.Create(
            "enemy-shadow-wolf",
            "Loup d’Ombre",
            "Une entité née dans un couloir du Palais.",
            "catalog-0.1.0");
    }

    private sealed class TestCatalogContent : CatalogContentBase
    {
        private TestCatalogContent(
            CatalogContentId id,
            CatalogContentKey key,
            CatalogContentName name,
            CatalogContentDescription description,
            CatalogContentVersion version,
            CatalogContentStatus status)
            : base(id, key, name, description, version, status)
        {
        }

        public static TestCatalogContent Create(
            string key,
            string name,
            string? description,
            string version,
            CatalogContentStatus status = CatalogContentStatus.Draft)
        {
            return new TestCatalogContent(
                CatalogContentId.New(),
                CatalogContentKey.From(key),
                CatalogContentName.From(name),
                CatalogContentDescription.From(description),
                CatalogContentVersion.From(version),
                status);
        }
    }
}