using FluentAssertions;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerCharacterIdTests
{
    [Fact]
    public void New_ShouldGenerateUniqueIds()
    {
        var id1 = PlayerCharacterId.New();
        var id2 = PlayerCharacterId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void New_ShouldGenerateNonEmptyGuid()
    {
        var id = PlayerCharacterId.New();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void New_ShouldGenerateValidGuid()
    {
        var id = PlayerCharacterId.New();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_ShouldAcceptProvidedGuid()
    {
        var guid = Guid.NewGuid();

        var id = new PlayerCharacterId(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_ShouldAcceptEmptyGuid()
    {
        var id = new PlayerCharacterId(Guid.Empty);

        id.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();
        var id = new PlayerCharacterId(guid);

        id.ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Equality_ShouldBeTrueForSameValue()
    {
        var guid = Guid.NewGuid();
        var id1 = new PlayerCharacterId(guid);
        var id2 = new PlayerCharacterId(guid);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_ShouldBeFalseForDifferentValues()
    {
        var id1 = PlayerCharacterId.New();
        var id2 = PlayerCharacterId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void GetHashCode_ShouldBeEqualForSameValue()
    {
        var guid = Guid.NewGuid();
        var id1 = new PlayerCharacterId(guid);
        var id2 = new PlayerCharacterId(guid);

        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void WithExpression_ShouldCreateModifiedCopy()
    {
        var id1 = PlayerCharacterId.New();
        var newGuid = Guid.NewGuid();

        var id2 = id1 with { Value = newGuid };

        id1.Should().NotBe(id2);
        id2.Value.Should().Be(newGuid);
    }
}
