using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.UnitTests.Domain.CatalogContent;

public sealed class CatalogVersionTests
{
    [Fact]
    public void From_ShouldCreateCatalogVersion_WhenValueIsValid()
    {
        var version = CatalogContentVersion.From("catalog-0.1.0");

        version.Value.Should().Be("catalog-0.1.0");
        version.ToString().Should().Be("catalog-0.1.0");
    }

    [Fact]
    public void From_ShouldTrimValue_WhenValueContainsWhitespaces()
    {
        var version = CatalogContentVersion.From("  catalog-0.1.0  ");

        version.Value.Should().Be("catalog-0.1.0");
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenValueIsEmpty()
    {
        var act = () => CatalogContentVersion.From(string.Empty);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Catalog version is required.");
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenValueIsWhitespace()
    {
        var act = () => CatalogContentVersion.From("   ");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Catalog version is required.");
    }
}