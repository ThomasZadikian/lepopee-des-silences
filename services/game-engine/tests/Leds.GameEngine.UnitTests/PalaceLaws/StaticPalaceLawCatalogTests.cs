using FluentAssertions;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.PalaceLaws;

public sealed class StaticPalaceLawCatalogTests
{
    [Fact]
    public void GetDefaultLawFor_ShouldReturnLawWithCorrectKey()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "accept-law");

        var sut = new StaticPalaceLawCatalog();

        var result = sut.GetDefaultLawFor(context);

        result.Key.Should().Be("law-silence-v1");
        result.Name.Should().Be("Loi du Silence");
        result.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void GetDefaultLawFor_ShouldReturnLawWithExpectedDomains()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "accept-law");

        var sut = new StaticPalaceLawCatalog();

        var result = sut.GetDefaultLawFor(context);

        result.Domains.Should().Contain(PalaceLawDomain.Generation);
        result.Domains.Should().Contain(PalaceLawDomain.Combat);
        result.Domains.Should().Contain(PalaceLawDomain.Rewards);
    }

    [Fact]
    public void GetDefaultLawFor_ShouldReturnLawWithExpectedEffects()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "accept-law");

        var sut = new StaticPalaceLawCatalog();

        var result = sut.GetDefaultLawFor(context);

        result.Effects.Should().HaveCount(2);

        var difficultyEffect = result.Effects.Single(e =>
            e.ModifierType == RunModifierType.PermanentCombatDifficultyBonus);
        difficultyEffect.Value.Should().Be(0.10);
        difficultyEffect.Duration.Should().Be(RunModifierDuration.UntilRunEnds);

        var rewardEffect = result.Effects.Single(e =>
            e.ModifierType == RunModifierType.RewardPowerMultiplierBonus);
        rewardEffect.Value.Should().Be(0.15);
        rewardEffect.Duration.Should().Be(RunModifierDuration.UntilRunEnds);
    }

    [Fact]
    public void GetDefaultLawFor_ShouldReturnConsistentLaw_RegardlessOfContext()
    {
        var runWithNode1 = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var runWithNode2 = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Combat);

        var context1 = new CurrentEventChoiceResolutionContext(
            runWithNode1.Run, runWithNode1.Run.CurrentRoom, runWithNode1.TargetNode, "choice1");
        var context2 = new CurrentEventChoiceResolutionContext(
            runWithNode2.Run, runWithNode2.Run.CurrentRoom, runWithNode2.TargetNode, "choice2");

        var sut = new StaticPalaceLawCatalog();

        var law1 = sut.GetDefaultLawFor(context1);
        var law2 = sut.GetDefaultLawFor(context2);

        law1.Key.Should().Be(law2.Key);
        law1.Name.Should().Be(law2.Name);
        law1.Effects.Should().HaveSameCount(law2.Effects);
    }
}
