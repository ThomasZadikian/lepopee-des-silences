using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerCharacterSkillTests
{
    [Fact]
    public void Create_ShouldCreateValidSkill()
    {
        var now = DateTimeOffset.UtcNow;
        var skill = PlayerCharacterSkill.Create("skill.basic.strike", now, "default");

        skill.SkillDefinitionKey.Should().Be("skill.basic.strike");
        skill.UnlockedAtUtc.Should().Be(now);
        skill.Source.Should().Be("default");
    }

    [Fact]
    public void Create_ShouldCreateSkillWithNullSource()
    {
        var now = DateTimeOffset.UtcNow;
        var skill = PlayerCharacterSkill.Create("skill.basic.strike", now, null);

        skill.Source.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCreateSkillWithEmptySource()
    {
        var now = DateTimeOffset.UtcNow;
        var skill = PlayerCharacterSkill.Create("skill.basic.strike", now, "");

        skill.Source.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCreateSkillWithWhitespaceSource()
    {
        var now = DateTimeOffset.UtcNow;
        var skill = PlayerCharacterSkill.Create("skill.basic.strike", now, "   ");

        skill.Source.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimSkillDefinitionKey()
    {
        var now = DateTimeOffset.UtcNow;
        var skill = PlayerCharacterSkill.Create("  skill.basic.strike  ", now, "default");

        skill.SkillDefinitionKey.Should().Be("skill.basic.strike");
    }

    [Fact]
    public void Create_ShouldTrimSource()
    {
        var now = DateTimeOffset.UtcNow;
        var skill = PlayerCharacterSkill.Create("skill.basic.strike", now, "  default  ");

        skill.Source.Should().Be("default");
    }

    [Fact]
    public void Create_ShouldRejectNullSkillDefinitionKey()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => PlayerCharacterSkill.Create(null!, now, "default");

        act.Should().Throw<DomainException>().WithMessage("*Skill definition key*");
    }

    [Fact]
    public void Create_ShouldRejectEmptySkillDefinitionKey()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => PlayerCharacterSkill.Create("", now, "default");

        act.Should().Throw<DomainException>().WithMessage("*Skill definition key*");
    }

    [Fact]
    public void Create_ShouldRejectWhitespaceSkillDefinitionKey()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => PlayerCharacterSkill.Create("   ", now, "default");

        act.Should().Throw<DomainException>().WithMessage("*Skill definition key*");
    }

    [Fact]
    public void Create_ShouldAcceptDefaultSource()
    {
        var now = DateTimeOffset.UtcNow;

        var skill = PlayerCharacterSkill.Create("skill.basic.strike", now);

        skill.Source.Should().BeNull();
    }

    [Theory]
    [InlineData("default")]
    [InlineData("reward")]
    [InlineData("legacy_migration")]
    [InlineData("unlock")]
    public void Create_ShouldAcceptVariousSources(string source)
    {
        var now = DateTimeOffset.UtcNow;

        var skill = PlayerCharacterSkill.Create("skill.basic.strike", now, source);

        skill.Source.Should().Be(source);
    }

    [Fact]
    public void Properties_ShouldBeImmutable()
    {
        var now = DateTimeOffset.UtcNow;
        var skill = PlayerCharacterSkill.Create("skill.basic.strike", now, "default");

        skill.SkillDefinitionKey.Should().Be("skill.basic.strike");
        skill.UnlockedAtUtc.Should().Be(now);
        skill.Source.Should().Be("default");
    }

    [Fact]
    public void Create_ShouldPreserveUtcTimestamp()
    {
        var specificTime = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

        var skill = PlayerCharacterSkill.Create("skill.basic.strike", specificTime, "default");

        skill.UnlockedAtUtc.Should().Be(specificTime);
    }
}
