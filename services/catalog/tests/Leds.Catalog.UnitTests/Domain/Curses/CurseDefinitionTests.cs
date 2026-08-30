using FluentAssertions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Curses;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.UnitTests.Domain.Curses;

public sealed class CurseDefinitionTests
{
    [Fact]
    public void Create_ShouldCreateCurseDefinition_WhenAllParametersAreValid()
    {
        var curse = CurseDefinition.Create(
            "curse-shadow-blight",
            "Fléau d'Ombre",
            "Une malédiction qui ronge la lumière intérieure.",
            "catalog-0.1.0",
            "L'ombre s'infiltre dans chaque recoin de l'esprit.",
            severity: 5,
            duration: "NextCombatOnly",
            trigger: "OnCombatStart",
            effectSetKey: "effect-shadow-blight");

        curse.Key.Value.Should().Be("curse-shadow-blight");
        curse.Name.Value.Should().Be("Fléau d'Ombre");
        curse.Description.Value.Should().Be("Une malédiction qui ronge la lumière intérieure.");
        curse.Version.Value.Should().Be("catalog-0.1.0");
        curse.NarrativeText.Should().Be("L'ombre s'infiltre dans chaque recoin de l'esprit.");
        curse.Severity.Should().Be(5);
        curse.Duration.Should().Be("NextCombatOnly");
        curse.Trigger.Should().Be("OnCombatStart");
        curse.EffectSetKey.Should().Be("effect-shadow-blight");
        curse.Status.Should().Be(CatalogContentStatus.Draft);
        curse.IsDraft.Should().BeTrue();
        curse.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetStatusToActive_WhenStatusIsProvidedAsActive()
    {
        var curse = CurseDefinition.Create(
            "curse-active",
            "Malédiction Active",
            "Description",
            "catalog-0.1.0",
            narrativeText: null,
            severity: 3,
            duration: "Permanent",
            trigger: null,
            effectSetKey: null,
            status: CatalogContentStatus.Active);

        curse.Status.Should().Be(CatalogContentStatus.Active);
        curse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetDefaultDuration_WhenDurationIsEmpty()
    {
        var curse = CurseDefinition.Create(
            "curse-default-duration",
            "Test",
            "Description",
            "catalog-0.1.0",
            narrativeText: null,
            severity: 1,
            duration: string.Empty,
            trigger: null,
            effectSetKey: null);

        curse.Duration.Should().Be("NextCombatOnly");
    }

    [Fact]
    public void Create_ShouldSetDefaultDuration_WhenDurationIsWhitespace()
    {
        var curse = CurseDefinition.Create(
            "curse-whitespace-duration",
            "Test",
            "Description",
            "catalog-0.1.0",
            narrativeText: null,
            severity: 1,
            duration: "   ",
            trigger: null,
            effectSetKey: null);

        curse.Duration.Should().Be("NextCombatOnly");
    }

    [Fact]
    public void Create_ShouldTrimDuration_WhenDurationHasWhitespace()
    {
        var curse = CurseDefinition.Create(
            "curse-trim-duration",
            "Test",
            "Description",
            "catalog-0.1.0",
            narrativeText: null,
            severity: 1,
            duration: "  Permanent  ",
            trigger: null,
            effectSetKey: null);

        curse.Duration.Should().Be("Permanent");
    }

    [Fact]
    public void Create_ShouldSetNarrativeTextToNull_WhenNarrativeTextIsEmpty()
    {
        var curse = CurseDefinition.Create(
            "curse-null-narrative",
            "Test",
            "Description",
            "catalog-0.1.0",
            narrativeText: string.Empty,
            severity: 1,
            duration: "NextCombatOnly",
            trigger: null,
            effectSetKey: null);

        curse.NarrativeText.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetNarrativeTextToNull_WhenNarrativeTextIsWhitespace()
    {
        var curse = CurseDefinition.Create(
            "curse-whitespace-narrative",
            "Test",
            "Description",
            "catalog-0.1.0",
            narrativeText: "   ",
            severity: 1,
            duration: "NextCombatOnly",
            trigger: null,
            effectSetKey: null);

        curse.NarrativeText.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimNarrativeText_WhenNarrativeTextHasWhitespace()
    {
        var curse = CurseDefinition.Create(
            "curse-trim-narrative",
            "Test",
            "Description",
            "catalog-0.1.0",
            narrativeText: "  Narrative text  ",
            severity: 1,
            duration: "NextCombatOnly",
            trigger: null,
            effectSetKey: null);

        curse.NarrativeText.Should().Be("Narrative text");
    }

    [Fact]
    public void Create_ShouldSetTriggerToNull_WhenTriggerIsEmpty()
    {
        var curse = CurseDefinition.Create(
            "curse-null-trigger",
            "Test",
            "Description",
            "catalog-0.1.0",
            narrativeText: null,
            severity: 1,
            duration: "NextCombatOnly",
            trigger: string.Empty,
            effectSetKey: null);

        curse.Trigger.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetEffectSetKeyToNull_WhenEffectSetKeyIsEmpty()
    {
        var curse = CurseDefinition.Create(
            "curse-null-effect",
            "Test",
            "Description",
            "catalog-0.1.0",
            narrativeText: null,
            severity: 1,
            duration: "NextCombatOnly",
            trigger: null,
            effectSetKey: string.Empty);

        curse.EffectSetKey.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimOptionalFields_WhenValuesHaveWhitespace()
    {
        var curse = CurseDefinition.Create(
            "curse-trim-all",
            "Test",
            "Description",
            "catalog-0.1.0",
            narrativeText: "  Narrative  ",
            severity: 2,
            duration: "  RunOnly  ",
            trigger: "  OnHit  ",
            effectSetKey: "  effect-set  ");

        curse.NarrativeText.Should().Be("Narrative");
        curse.Duration.Should().Be("RunOnly");
        curse.Trigger.Should().Be("OnHit");
        curse.EffectSetKey.Should().Be("effect-set");
    }

    [Fact]
    public void Create_ShouldAssignNewId_WhenCreated()
    {
        var curse1 = CurseDefinition.Create(
            "curse-1", "Test", "Desc", "v1", null, 1, "NextCombatOnly", null, null);

        var curse2 = CurseDefinition.Create(
            "curse-2", "Test", "Desc", "v1", null, 1, "NextCombatOnly", null, null);

        curse1.Id.Should().NotBe(curse2.Id);
        curse1.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenKeyIsEmpty()
    {
        var act = () => CurseDefinition.Create(
            string.Empty, "Test", "Desc", "v1", null, 1, "NextCombatOnly", null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("Catalog content key is required.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenNameIsEmpty()
    {
        var act = () => CurseDefinition.Create(
            "valid-key", string.Empty, "Desc", "v1", null, 1, "NextCombatOnly", null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("Catalog content name is required.");
    }

    [Fact]
    public void Create_ShouldSetDescriptionToEmpty_WhenDescriptionIsEmpty()
    {
        var curse = CurseDefinition.Create(
            "valid-key", "Test", string.Empty, "v1", null, 1, "NextCombatOnly", null, null);

        curse.Description.Value.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenVersionIsEmpty()
    {
        var act = () => CurseDefinition.Create(
            "valid-key", "Test", "Desc", string.Empty, null, 1, "NextCombatOnly", null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("Catalog version is required.");
    }

    [Fact]
    public void Activate_ShouldChangeStatusToActive_WhenStatusIsDraft()
    {
        var curse = CurseDefinition.Create(
            "curse-activate", "Test", "Desc", "v1", null, 1, "NextCombatOnly", null, null);

        curse.Activate();

        curse.Status.Should().Be(CatalogContentStatus.Active);
        curse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deprecate_ShouldChangeStatusToDeprecated_WhenStatusIsActive()
    {
        var curse = CurseDefinition.Create(
            "curse-deprecate", "Test", "Desc", "v1", null, 1, "NextCombatOnly", null, null,
            status: CatalogContentStatus.Active);

        curse.Deprecate();

        curse.Status.Should().Be(CatalogContentStatus.Deprecated);
        curse.IsDeprecated.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldChangeStatusToDisabled_WhenStatusIsDraft()
    {
        var curse = CurseDefinition.Create(
            "curse-disable", "Test", "Desc", "v1", null, 1, "NextCombatOnly", null, null);

        curse.Disable();

        curse.Status.Should().Be(CatalogContentStatus.Disabled);
        curse.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldThrowDomainException_WhenStatusIsDisabled()
    {
        var curse = CurseDefinition.Create(
            "curse-disabled", "Test", "Desc", "v1", null, 1, "NextCombatOnly", null, null);

        curse.Disable();

        var act = () => curse.Activate();

        act.Should().Throw<DomainException>()
            .WithMessage("Disabled catalog content cannot be activated.");
    }
}
