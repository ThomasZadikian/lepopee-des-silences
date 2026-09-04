using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.UnitTests.Domain.Items;

public sealed class EquipmentDefinitionMetadataTests
{
    [Theory]
    [InlineData("Head")]
    [InlineData("Ring")]
    [InlineData("MainWeapon")]
    [InlineData("OffWeapon")]
    public void ValidateAllowedSlots_ShouldAcceptCanonicalValues(string slot)
    {
        var act = () => EquipmentDefinitionMetadata.Validate([slot], null, ["light-weapon"]);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateAllowedSlots_ShouldRejectConcreteRingPosition()
    {
        var act = () => EquipmentDefinitionMetadata.Validate(["Ring1"], null, []);
        act.Should().Throw<DomainException>().WithMessage("*Ring1*");
    }

    [Theory]
    [InlineData("Upper Body")]
    [InlineData("plate!")]
    public void ValidateProficiencies_ShouldRejectInvalidTag(string tag)
    {
        var act = () => EquipmentDefinitionMetadata.Validate(["Chest"], null, [tag]);
        act.Should().Throw<DomainException>().WithMessage("*proficiency*");
    }
}
