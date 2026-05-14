using FluentAssertions;
using RPG_ESI07.Application.Guards;

namespace RPG_ESI07.Tests.Application.Guards;

public class GuardTests
{
    [Fact]
    public void ThrowIfNull_ThrowsArgumentNullException_WhenNull()
    {
        object target = null!;
        Action act = () => Guard.ThrowIfNull(target, "target");
        act.Should().Throw<ArgumentNullException>().WithParameterName("target");
    }

    [Fact]
    public void ThrowIfNull_DoesNotThrow_WhenNotNull()
    {
        object target = new();
        Action act = () => Guard.ThrowIfNull(target, "target");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    [InlineData("   ")]
    public void ThrowIfNullOrEmpty_ThrowsArgumentException_WhenInvalid(string value)
    {
        Action act = () => Guard.ThrowIfNullOrEmpty(value, nameof(value));
        act.Should().Throw<ArgumentException>().WithParameterName(nameof(value));
    }

    [Fact]
    public void ThrowIfNegative_ThrowsArgumentException_WhenNegative()
    {
        Action act = () => Guard.ThrowIfNegative(-1, "value");
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void ThrowIfZeroOrNegative_ThrowsArgumentException_WhenZero()
    {
        Action act = () => Guard.ThrowIfZeroOrNegative(0, "value");
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void ThrowIf_ThrowsInvalidOperationException_WhenConditionMet()
    {
        Action act = () => Guard.ThrowIf(true, "Error state");
        act.Should().Throw<InvalidOperationException>().WithMessage("Error state");
    }

    [Fact]
    public void ThrowIfEmpty_ThrowsArgumentException_WhenCollectionEmpty()
    {
        var list = new List<string>();
        Action act = () => Guard.ThrowIfEmpty(list, "list");
        act.Should().Throw<ArgumentException>().WithParameterName("list");
    }
}