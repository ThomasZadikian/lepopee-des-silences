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

    [Fact]
    public void SystemClock_ShouldImplementIClock()
    {
        IClock clock = new SystemClock();

        clock.Should().NotBeNull();
        clock.Should().BeOfType<SystemClock>();
    }

    [Fact]
    public void UtcNow_ShouldReturnConsistentOffset()
    {
        var clock = new SystemClock();

        var now = clock.UtcNow;

        now.Offset.Should().Be(TimeSpan.Zero);
        now.UtcDateTime.Should().Be(now.DateTime);
    }

    [Fact]
    public void UtcNow_MultipleCalls_ShouldReturnIncreasingTimes()
    {
        var clock = new SystemClock();

        var first = clock.UtcNow;
        var second = clock.UtcNow;

        second.Should().BeOnOrAfter(first);
    }
}
