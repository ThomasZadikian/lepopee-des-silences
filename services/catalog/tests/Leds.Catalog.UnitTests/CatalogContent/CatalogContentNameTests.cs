using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.UnitTests.CatalogContent;

public sealed class CatalogContentNameTests
{
    [Fact]
    public void From_ShouldCreateName_WhenValueIsValid()
    {
        var name = CatalogContentName.From("Loup d’Ombre");

        name.Value.Should().Be("Loup d’Ombre");
        name.ToString().Should().Be("Loup d’Ombre");
    }

    [Fact]
    public void From_ShouldTrimValue_WhenValueContainsWhitespaces()
    {
        var name = CatalogContentName.From("  Loup d’Ombre  ");

        name.Value.Should().Be("Loup d’Ombre");
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenValueIsEmpty()
    {
        var act = () => CatalogContentName.From(string.Empty);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Catalog content name is required.");
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenValueIsWhitespace()
    {
        var act = () => CatalogContentName.From("   ");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Catalog content name is required.");
    }
}