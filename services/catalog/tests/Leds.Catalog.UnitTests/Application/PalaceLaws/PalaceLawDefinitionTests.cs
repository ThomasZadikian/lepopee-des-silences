using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.PalaceLaws;

namespace Leds.Catalog.UnitTests.Application.PalaceLaws;

public sealed class PalaceLawDefinitionTests
{
    [Fact]
    public void Create_ShouldCreatePalaceLawDefinition_WhenInputIsValid()
    {
        var law = PalaceLawDefinition.Create(
            "law-silence-v1",
            "Loi du Silence",
            "Le silence déforme la génération et modifie les fragments narratifs.",
            "law-1.0.0",
            PalaceLawVisibility.PartiallyVisible,
            priority: 10,
            new[]
            {
                PalaceLawImpactDomain.Generation,
                PalaceLawImpactDomain.Narrative
            });

        law.Id.Value.Should().NotBeEmpty();
        law.Key.Value.Should().Be("law-silence-v1");
        law.Name.Value.Should().Be("Loi du Silence");
        law.Description.Value.Should().Be(
            "Le silence déforme la génération et modifie les fragments narratifs.");
        law.Version.Value.Should().Be("law-1.0.0");
        law.Status.Should().Be(CatalogContentStatus.Draft);
        law.Visibility.Should().Be(PalaceLawVisibility.PartiallyVisible);
        law.Priority.Should().Be(10);
        law.ImpactDomains.Should().Contain(PalaceLawImpactDomain.Generation);
        law.ImpactDomains.Should().Contain(PalaceLawImpactDomain.Narrative);
    }

    [Fact]
    public void Create_ShouldRemoveDuplicateImpactDomains()
    {
        var law = PalaceLawDefinition.Create(
            "law-silence-v1",
            "Loi du Silence",
            "Description",
            "law-1.0.0",
            PalaceLawVisibility.Visible,
            priority: 10,
            new[]
            {
                PalaceLawImpactDomain.Generation,
                PalaceLawImpactDomain.Generation
            });

        law.ImpactDomains.Should().ContainSingle();
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenPriorityIsNegative()
    {
        var act = () => PalaceLawDefinition.Create(
            "law-silence-v1",
            "Loi du Silence",
            "Description",
            "law-1.0.0",
            PalaceLawVisibility.Visible,
            priority: -1,
            new[] { PalaceLawImpactDomain.Generation });

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Palace law priority cannot be negative.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenNoImpactDomainIsProvided()
    {
        var act = () => PalaceLawDefinition.Create(
            "law-silence-v1",
            "Loi du Silence",
            "Description",
            "law-1.0.0",
            PalaceLawVisibility.Visible,
            priority: 10,
            Array.Empty<PalaceLawImpactDomain>());

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Palace law must target at least one impact domain.");
    }
}