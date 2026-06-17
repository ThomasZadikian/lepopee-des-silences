using FluentAssertions;
using Leds.SharedBuildingBlocks.Time;
using Xunit;

namespace Leds.SharedBuildingBlocks.UnitTests.Time;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        var clock = new SystemClock();

        var before = DateTimeOffset.UtcNow;
        var current = clock.UtcNow;
        var after = DateTimeOffset.UtcNow;

        current.Should().BeOnOrAfter(before);
        current.Should().BeOnOrBefore(after);
        current.Offset.Should().Be(TimeSpan.Zero);
    }
}
