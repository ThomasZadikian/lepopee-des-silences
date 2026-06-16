using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Players;

public sealed class PlayerDataModelDomainTests
{
    [Fact]
    public void DefaultPorteurStatBlock_ShouldUseDataModelDefaults()
    {
        var statBlock = PlayerCharacterStatBlock.CreateDefaultPorteur();

        statBlock.MaxVitality.Should().Be(100);
        statBlock.AttackPower.Should().Be(12);
        statBlock.Defense.Should().Be(6);
        statBlock.StartingGuard.Should().Be(0);
        statBlock.Speed.Should().Be(10);
        statBlock.Initiative.Should().Be(10);
        statBlock.Recovery.Should().Be(5);
    }

    [Fact]
    public void PlayerCharacter_AddSkill_ShouldIgnoreDuplicates()
    {
        var now = DateTimeOffset.UtcNow;
        var character = PlayerCharacter.Create(
            "character.player.self",
            "Le Porteur",
            PlayerCharacterStatBlock.CreateDefaultPorteur(),
            [PlayerCharacterSkill.Create("skill.basic.strike", now, "default")]);

        character.AddSkill(PlayerCharacterSkill.Create("skill.basic.strike", now, "reward"));

        character.Skills.Should().HaveCount(1);
    }

    [Fact]
    public void PlayerPermanentUnlock_Create_ShouldRejectEmptyKey()
    {
        var act = () => PlayerPermanentUnlock.Create("", "Skill", null, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*Unlock key*");
    }

    [Fact]
    public void PlayerRunStatistic_Create_ShouldRejectEmptyRunId()
    {
        var act = () => PlayerRunStatistic.Create(Guid.Empty, "seed", 1, "Completed", "gen");

        act.Should().Throw<DomainException>().WithMessage("*Run id*");
    }
}
