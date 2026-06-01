using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.UnitTests.CatalogContent;

public sealed class CatalogContentKeyTests
{
    [Fact]
    public void From_ShouldCreateKey_WhenValueIsValid()
    {
        var key = CatalogContentKey.From("enemy-shadow-wolf");

        key.Value.Should().Be("enemy-shadow-wolf");
        key.ToString().Should().Be("enemy-shadow-wolf");
    }

    [Fact]
    public void From_ShouldTrimValue_WhenValueContainsWhitespaces()
    {
        var key = CatalogContentKey.From("  enemy-shadow-wolf  ");

        key.Value.Should().Be("enemy-shadow-wolf");
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenValueIsEmpty()
    {
        var act = () => CatalogContentKey.From(string.Empty);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Catalog content key is required.");
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenValueIsWhitespace()
    {
        var act = () => CatalogContentKey.From("   ");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Catalog content key is required.");
    }
}