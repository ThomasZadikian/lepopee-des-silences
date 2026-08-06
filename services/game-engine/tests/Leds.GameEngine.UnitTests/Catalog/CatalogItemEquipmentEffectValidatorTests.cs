using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogItemEquipmentEffectValidatorTests
{
    [Fact]
    public void Validate_ShouldRejectIncompleteStatEffectAtTheServiceBoundary()
    {
        var effects = new[]
        {
            new CatalogItemEquipmentEffect("StatBonusPercent", "Speed", null, null, null)
        };

        var act = () => CatalogItemEquipmentEffectValidator.Validate("item.invalid", effects);

        act.Should().Throw<DomainException>().WithMessage("*requires Amount*");
    }

    [Fact]
    public void Validate_ShouldRejectUnsupportedRuntimeBehaviorAtTheServiceBoundary()
    {
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "RuntimeBehavior", null, null, null, null,
                BehaviorCode: "unknown-handler")
        };

        var act = () => CatalogItemEquipmentEffectValidator.Validate("item.invalid", effects);

        act.Should().Throw<DomainException>().WithMessage("*not supported*");
    }
}
