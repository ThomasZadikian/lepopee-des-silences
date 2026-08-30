using FluentAssertions;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerIdTests
{
    [Fact]
    public void New_ShouldGenerateUniqueIds()
    {
        var id1 = PlayerId.New();
        var id2 = PlayerId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void New_ShouldGenerateNonEmptyGuid()
    {
        var id = PlayerId.New();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void New_ShouldGenerateValidGuid()
    {
        var id = PlayerId.New();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_ShouldAcceptProvidedGuid()
    {
        var guid = Guid.NewGuid();

        var id = new PlayerId(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_ShouldAcceptEmptyGuid()
    {
        var id = new PlayerId(Guid.Empty);

        id.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();
        var id = new PlayerId(guid);

        id.ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Equality_ShouldBeTrueForSameValue()
    {
        var guid = Guid.NewGuid();
        var id1 = new PlayerId(guid);
        var id2 = new PlayerId(guid);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_ShouldBeFalseForDifferentValues()
    {
        var id1 = PlayerId.New();
        var id2 = PlayerId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void GetHashCode_ShouldBeEqualForSameValue()
    {
        var guid = Guid.NewGuid();
        var id1 = new PlayerId(guid);
        var id2 = new PlayerId(guid);

        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void WithExpression_ShouldCreateModifiedCopy()
    {
        var id1 = PlayerId.New();
        var newGuid = Guid.NewGuid();

        var id2 = id1 with { Value = newGuid };

        id1.Should().NotBe(id2);
        id2.Value.Should().Be(newGuid);
    }
}
