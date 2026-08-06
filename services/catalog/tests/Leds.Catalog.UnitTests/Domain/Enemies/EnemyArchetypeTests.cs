using FluentAssertions;
using Leds.Catalog.Domain.Enemies;

namespace Leds.Catalog.UnitTests.Domain.Enemies;

public sealed class EnemyArchetypeCatalogTests
{
    [Fact]
    public void ShouldPublishTheCanonicalCodes()
    {
        EnemyArchetypeCatalog.All.Should().BeEquivalentTo(
        [
            "Beast", "Boss", "Bruiser", "Disruptor", "Elite", "Fragile", "Guard",
            "Memory", "Rupture", "Shadow", "Skirmisher", "Support", "Tank", "Trauma"
        ]);
    }

    [Theory]
    [InlineData("boss", "Boss")]
    [InlineData(" GUARD ", "Guard")]
    public void Parse_ShouldReturnCanonicalCode(string input, string expected)
    {
        EnemyArchetypeCatalog.Parse(input).Should().Be(expected);
    }

    [Fact]
    public void Parse_ShouldRejectUnknownCode()
    {
        var act = () => EnemyArchetypeCatalog.Parse("Guardian");
        act.Should().Throw<Exception>().WithMessage("*Unknown enemy archetype*");
    }
}
