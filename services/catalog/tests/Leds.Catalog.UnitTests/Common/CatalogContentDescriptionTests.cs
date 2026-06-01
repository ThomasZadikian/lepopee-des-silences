using FluentAssertions;
using Leds.Catalog.Domain.Common;

namespace Leds.Catalog.UnitTests.Common;

public sealed class CatalogContentDescriptionTests
{
    [Fact]
    public void From_ShouldCreateDescription_WhenValueIsValid()
    {
        var description = CatalogContentDescription.From("Une entité née dans un couloir du Palais.");

        description.Value.Should().Be("Une entité née dans un couloir du Palais.");
        description.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void From_ShouldTrimValue_WhenValueContainsWhitespaces()
    {
        var description = CatalogContentDescription.From("  Description  ");

        description.Value.Should().Be("Description");
    }

    [Fact]
    public void From_ShouldCreateEmptyDescription_WhenValueIsNull()
    {
        var description = CatalogContentDescription.From(null);

        description.Value.Should().BeEmpty();
        description.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void From_ShouldCreateEmptyDescription_WhenValueIsWhitespace()
    {
        var description = CatalogContentDescription.From("   ");

        description.Value.Should().BeEmpty();
        description.IsEmpty.Should().BeTrue();
    }
}