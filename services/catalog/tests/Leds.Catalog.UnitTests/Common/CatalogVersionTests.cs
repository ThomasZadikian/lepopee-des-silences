using FluentAssertions;
using Leds.Catalog.Domain.Common;

namespace Leds.Catalog.UnitTests.Common;

public sealed class CatalogVersionTests
{
    [Fact]
    public void From_ShouldCreateCatalogVersion_WhenValueIsValid()
    {
        var version = CatalogVersion.From("catalog-0.1.0");

        version.Value.Should().Be("catalog-0.1.0");
        version.ToString().Should().Be("catalog-0.1.0");
    }

    [Fact]
    public void From_ShouldTrimValue_WhenValueContainsWhitespaces()
    {
        var version = CatalogVersion.From("  catalog-0.1.0  ");

        version.Value.Should().Be("catalog-0.1.0");
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenValueIsEmpty()
    {
        var act = () => CatalogVersion.From(string.Empty);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Catalog version is required.");
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenValueIsWhitespace()
    {
        var act = () => CatalogVersion.From("   ");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Catalog version is required.");
    }
}