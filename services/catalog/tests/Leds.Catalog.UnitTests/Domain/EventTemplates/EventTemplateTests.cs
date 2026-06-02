using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.EventTemplates;

namespace Leds.Catalog.UnitTests.Domain.EventTemplates;

public sealed class EventTemplateTests
{
    [Fact]
    public void Create_ShouldCreateEventTemplate_WhenInputIsValid()
    {
        var template = EventTemplate.Create(
            "event-memory-threshold-v1",
            "Mémoire du seuil",
            "Une mémoire apparaît à l’entrée du Palais.",
            "event-1.0.0",
            EventTemplateType.Memory,
            EventOutcomeKind.TomePageUnlocked,
            minRiskLevel: 5,
            maxRiskLevel: 20,
            requiresPlayerChoice: true,
            new[]
            {
                "memory",
                "threshold",
                "elise"
            });

        template.Id.Value.Should().NotBeEmpty();
        template.Key.Value.Should().Be("event-memory-threshold-v1");
        template.Name.Value.Should().Be("Mémoire du seuil");
        template.Description.Value.Should().Be("Une mémoire apparaît à l’entrée du Palais.");
        template.Version.Value.Should().Be("event-1.0.0");
        template.Status.Should().Be(CatalogContentStatus.Draft);
        template.Type.Should().Be(EventTemplateType.Memory);
        template.DefaultOutcomeKind.Should().Be(EventOutcomeKind.TomePageUnlocked);
        template.MinRiskLevel.Should().Be(5);
        template.MaxRiskLevel.Should().Be(20);
        template.RequiresPlayerChoice.Should().BeTrue();
        template.NarrativeTags.Should().BeEquivalentTo("memory", "threshold", "elise");
    }

    [Fact]
    public void Create_ShouldCreateEventTemplate_WithEmptyNarrativeTags_WhenTagsAreNull()
    {
        var template = EventTemplate.Create(
            "event-combat-shadow-v1",
            "Combat d’ombre",
            "Un combat mineur dans le Palais.",
            "event-1.0.0",
            EventTemplateType.Combat,
            EventOutcomeKind.CombatStarted,
            minRiskLevel: 10,
            maxRiskLevel: 40,
            requiresPlayerChoice: false,
            narrativeTags: null);

        template.NarrativeTags.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldNormalizeNarrativeTags()
    {
        var template = EventTemplate.Create(
            "event-memory-threshold-v1",
            "Mémoire du seuil",
            "Description",
            "event-1.0.0",
            EventTemplateType.Memory,
            EventOutcomeKind.TomePageUnlocked,
            minRiskLevel: 5,
            maxRiskLevel: 20,
            requiresPlayerChoice: true,
            new[]
            {
                " memory ",
                "Memory",
                "",
                "elise",
                "   "
            });

        template.NarrativeTags.Should().BeEquivalentTo("memory", "elise");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenMinRiskLevelIsNegative()
    {
        var act = () => EventTemplate.Create(
            "event-memory-threshold-v1",
            "Mémoire du seuil",
            "Description",
            "event-1.0.0",
            EventTemplateType.Memory,
            EventOutcomeKind.TomePageUnlocked,
            minRiskLevel: -1,
            maxRiskLevel: 20,
            requiresPlayerChoice: true);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Event template min risk level must be between 0 and 100.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenMaxRiskLevelIsGreaterThan100()
    {
        var act = () => EventTemplate.Create(
            "event-memory-threshold-v1",
            "Mémoire du seuil",
            "Description",
            "event-1.0.0",
            EventTemplateType.Memory,
            EventOutcomeKind.TomePageUnlocked,
            minRiskLevel: 5,
            maxRiskLevel: 101,
            requiresPlayerChoice: true);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Event template max risk level must be between 0 and 100.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenMinRiskLevelIsGreaterThanMaxRiskLevel()
    {
        var act = () => EventTemplate.Create(
            "event-memory-threshold-v1",
            "Mémoire du seuil",
            "Description",
            "event-1.0.0",
            EventTemplateType.Memory,
            EventOutcomeKind.TomePageUnlocked,
            minRiskLevel: 50,
            maxRiskLevel: 20,
            requiresPlayerChoice: true);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Event template min risk level cannot be greater than max risk level.");
    }
}