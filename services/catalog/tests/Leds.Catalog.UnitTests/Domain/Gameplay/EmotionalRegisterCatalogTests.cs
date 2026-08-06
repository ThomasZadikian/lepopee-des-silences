using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Gameplay;

public sealed class EmotionalRegisterCatalogTests
{
    [Theory]
    [InlineData("silence", EmotionalRegister.Silence)]
    [InlineData("Silence", EmotionalRegister.Silence)]
    [InlineData("melancolie", EmotionalRegister.Melancolie)]
    [InlineData("Melancolie", EmotionalRegister.Melancolie)]
    public void Parse_ShouldResolveCanonicalCodeAndLegacyName(
        string value,
        EmotionalRegister expected)
    {
        EmotionalRegisterCatalog.Parse(value).Should().Be(expected);
    }

    [Fact]
    public void Parse_ShouldRejectUnknownRegister()
    {
        var act = () => EmotionalRegisterCatalog.Parse("unknown");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Codes_ShouldBeUniqueAndStable()
    {
        EmotionalRegisterCatalog.All.Select(d => d.Code)
            .Should().OnlyHaveUniqueItems()
            .And.OnlyContain(code => code == code.ToLowerInvariant());
    }
}
