using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;

namespace Leds.Catalog.UnitTests.Domain.CatalogContent;

public sealed class CatalogContentIdTests
{
    [Fact]
    public void New_ShouldGenerateUniqueGuid_WhenCalled()
    {
        var id1 = CatalogContentId.New();
        var id2 = CatalogContentId.New();

        id1.Value.Should().NotBe(Guid.Empty);
        id2.Value.Should().NotBe(Guid.Empty);
        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact]
    public void New_ShouldReturnValidCatalogContentId()
    {
        var id = CatalogContentId.New();

        id.Should().BeOfType<CatalogContentId>();
        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ToString_ShouldReturnGuidStringRepresentation()
    {
        var id = CatalogContentId.New();

        id.ToString().Should().Be(id.Value.ToString());
    }

    [Fact]
    public void Value_ShouldBeAccessible_WhenCreatedDirectly()
    {
        var guid = Guid.NewGuid();
        var id = new CatalogContentId(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Equality_ShouldBeEqual_WhenValuesMatch()
    {
        var guid = Guid.NewGuid();
        var id1 = new CatalogContentId(guid);
        var id2 = new CatalogContentId(guid);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_ShouldNotBeEqual_WhenValuesDiffer()
    {
        var id1 = CatalogContentId.New();
        var id2 = CatalogContentId.New();

        id1.Should().NotBe(id2);
    }
}
