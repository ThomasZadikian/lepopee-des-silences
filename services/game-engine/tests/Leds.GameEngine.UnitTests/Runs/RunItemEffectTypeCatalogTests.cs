using FluentAssertions;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunItemEffectTypeCatalogTests
{
    [Fact]
    public void ShouldHaveExactlyOneDefinitionPerEnumMember()
    {
        var enumValues = Enum.GetValues<RunItemEffectType>();

        RunItemEffectTypeCatalog.All.Should().HaveCount(enumValues.Length);
        RunItemEffectTypeCatalog.All.Select(d => d.Value).Should().BeEquivalentTo(enumValues);
    }

    [Fact]
    public void CodesShouldBeUniqueAndComplete()
    {
        RunItemEffectTypeCatalog.All.Should().OnlyHaveUniqueItems(d => d.Code);
        RunItemEffectTypeCatalog.All.Should().OnlyContain(d =>
            !string.IsNullOrWhiteSpace(d.Code)
            && !string.IsNullOrWhiteSpace(d.DisplayName)
            && !string.IsNullOrWhiteSpace(d.Glyph)
            && !string.IsNullOrWhiteSpace(d.Color));
    }
}
