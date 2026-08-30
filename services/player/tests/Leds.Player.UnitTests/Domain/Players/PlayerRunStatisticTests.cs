using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerRunStatisticTests
{
    [Fact]
    public void Create_ShouldCreateValidStatistic()
    {
        var runId = Guid.NewGuid();
        var statistic = PlayerRunStatistic.Create(runId, "seed123", 5, "Completed", "v1.0.0");

        statistic.RunId.Should().Be(runId);
        statistic.Seed.Should().Be("seed123");
        statistic.FinalDepth.Should().Be(5);
        statistic.Outcome.Should().Be("Completed");
        statistic.GeneratorVersion.Should().Be("v1.0.0");
    }

    [Fact]
    public void Create_ShouldTrimSeed()
    {
        var statistic = PlayerRunStatistic.Create(Guid.NewGuid(), "  seed123  ", 5, "Completed", "v1.0.0");

        statistic.Seed.Should().Be("seed123");
    }

    [Fact]
    public void Create_ShouldTrimOutcome()
    {
        var statistic = PlayerRunStatistic.Create(Guid.NewGuid(), "seed", 5, "  Completed  ", "v1.0.0");

        statistic.Outcome.Should().Be("Completed");
    }

    [Fact]
    public void Create_ShouldTrimGeneratorVersion()
    {
        var statistic = PlayerRunStatistic.Create(Guid.NewGuid(), "seed", 5, "Completed", "  v1.0.0  ");

        statistic.GeneratorVersion.Should().Be("v1.0.0");
    }

    [Fact]
    public void Create_ShouldRejectEmptyRunId()
    {
        var act = () => PlayerRunStatistic.Create(Guid.Empty, "seed", 5, "Completed", "v1.0.0");

        act.Should().Throw<DomainException>().WithMessage("*Run id*");
    }

    [Fact]
    public void Create_ShouldRejectNullSeed()
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), null!, 5, "Completed", "v1.0.0");

        act.Should().Throw<DomainException>().WithMessage("*seed*");
    }

    [Fact]
    public void Create_ShouldRejectEmptySeed()
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), "", 5, "Completed", "v1.0.0");

        act.Should().Throw<DomainException>().WithMessage("*seed*");
    }

    [Fact]
    public void Create_ShouldRejectWhitespaceSeed()
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), "   ", 5, "Completed", "v1.0.0");

        act.Should().Throw<DomainException>().WithMessage("*seed*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectNegativeFinalDepth(int depth)
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), "seed", depth, "Completed", "v1.0.0");

        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_ShouldAllowZeroFinalDepth()
    {
        var statistic = PlayerRunStatistic.Create(Guid.NewGuid(), "seed", 0, "Completed", "v1.0.0");

        statistic.FinalDepth.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldRejectNullOutcome()
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), "seed", 5, null!, "v1.0.0");

        act.Should().Throw<DomainException>().WithMessage("*outcome*");
    }

    [Fact]
    public void Create_ShouldRejectEmptyOutcome()
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), "seed", 5, "", "v1.0.0");

        act.Should().Throw<DomainException>().WithMessage("*outcome*");
    }

    [Fact]
    public void Create_ShouldRejectWhitespaceOutcome()
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), "seed", 5, "   ", "v1.0.0");

        act.Should().Throw<DomainException>().WithMessage("*outcome*");
    }

    [Fact]
    public void Create_ShouldRejectNullGeneratorVersion()
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), "seed", 5, "Completed", null!);

        act.Should().Throw<DomainException>().WithMessage("*Generator version*");
    }

    [Fact]
    public void Create_ShouldRejectEmptyGeneratorVersion()
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), "seed", 5, "Completed", "");

        act.Should().Throw<DomainException>().WithMessage("*Generator version*");
    }

    [Fact]
    public void Create_ShouldRejectWhitespaceGeneratorVersion()
    {
        var act = () => PlayerRunStatistic.Create(Guid.NewGuid(), "seed", 5, "Completed", "   ");

        act.Should().Throw<DomainException>().WithMessage("*Generator version*");
    }

    [Fact]
    public void Properties_ShouldBeImmutable()
    {
        var runId = Guid.NewGuid();
        var statistic = PlayerRunStatistic.Create(runId, "seed", 5, "Completed", "v1.0.0");

        statistic.RunId.Should().Be(runId);
        statistic.Seed.Should().Be("seed");
        statistic.FinalDepth.Should().Be(5);
        statistic.Outcome.Should().Be("Completed");
        statistic.GeneratorVersion.Should().Be("v1.0.0");
    }
}
