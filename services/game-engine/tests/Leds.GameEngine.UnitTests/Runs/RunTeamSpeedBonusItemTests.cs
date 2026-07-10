using FluentAssertions;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunTeamSpeedBonusItemTests
{
    [Fact]
    public void AddRunItem_ShouldCreateSpeedBonusModifier_WhenItemGrantsTeamSpeedBonus()
    {
        var run = TestGameEngineFactory.CreateRun();
        var item = RunItem.Create(
            "canon.item.reve-erina", "Rêve d'Erina", "Description",
            RunItemType.Passive, RunItemRarity.Rare, 1,
            RunItemEffectType.TeamSpeedBonus, effectAmount: 5);

        run.AddRunItem(item);

        run.RunModifiers.Should().ContainSingle(
            m => m.Type == RunModifierType.SpeedBonus && !m.IsConsumed);
    }

    [Fact]
    public void AddRunItem_ShouldEncodePercentAsFraction_OnSpeedBonusModifier()
    {
        var run = TestGameEngineFactory.CreateRun();
        var item = RunItem.Create(
            "canon.item.reve-erina", "Rêve d'Erina", "Description",
            RunItemType.Passive, RunItemRarity.Rare, 1,
            RunItemEffectType.TeamSpeedBonus, effectAmount: 5);

        run.AddRunItem(item);

        var modifier = run.RunModifiers.Single(m => m.Type == RunModifierType.SpeedBonus);
        modifier.Value.Should().Be(0.05);
        modifier.Duration.Should().Be(RunModifierDuration.UntilRunEnds);
    }

    [Fact]
    public void AddRunItem_ShouldNotCreateAnyModifier_ForItemsWithoutASpecialEffectType()
    {
        var run = TestGameEngineFactory.CreateRun();
        var item = RunItem.Create(
            "canon.item.tome-38", "Le Tome 38", "Description",
            RunItemType.Passive, RunItemRarity.Rare, 1,
            RunItemEffectType.None, effectAmount: 0);

        run.AddRunItem(item);

        run.RunModifiers.Should().BeEmpty();
    }
}
