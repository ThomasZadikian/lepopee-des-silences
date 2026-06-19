using FluentAssertions;
using Leds.GameEngine.Infrastructure.Catalog;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogLawCurseProviderTests
{
    [Fact]
    public async Task PalaceLawProvider_ShouldReturnSeedLaw()
    {
        var gateway = new InMemoryCatalogContentGateway();

        var law = await gateway.GetPalaceLawDefinitionByKeyAsync("law-silence-v1");

        law.IsSuccess.Should().BeTrue();
        law.Value.Key.Should().Be("law-silence-v1");
        law.Value.EffectSetKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PalaceLawProvider_ShouldListAvailableLaws()
    {
        var gateway = new InMemoryCatalogContentGateway();

        var laws = await gateway.ListActivePalaceLawDefinitionsAsync();

        laws.Should().NotBeEmpty();
        laws.Should().Contain(l => l.Key == "law-silence-v1");
    }

    [Fact]
    public async Task CurseProvider_ShouldReturnSeedCurse()
    {
        var gateway = new InMemoryCatalogContentGateway();

        var curse = await gateway.GetCurseDefinitionByKeyAsync("curse.old-wound");

        curse.IsSuccess.Should().BeTrue();
        curse.Value.Key.Should().Be("curse.old-wound");
        curse.Value.DisplayName.Should().Be("Vieille blessure");
        curse.Value.Duration.Should().Be("NextCombatOnly");
        curse.Value.EffectSetKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CurseProvider_ShouldListAvailableCurses()
    {
        var gateway = new InMemoryCatalogContentGateway();

        var curses = await gateway.ListAvailableCurseDefinitionsAsync();

        curses.Should().NotBeEmpty();
        curses.Should().Contain(c => c.Key == "curse.old-wound");
    }

    [Fact]
    public async Task EffectSetProvider_ShouldReturnLawEffectSet()
    {
        var gateway = new InMemoryCatalogContentGateway();

        var effectSet = await gateway.GetEffectSetByKeyAsync("effectset.law-silence-v1");

        effectSet.IsSuccess.Should().BeTrue();
        effectSet.Value.Effects.Should().HaveCount(2);
        effectSet.Value.Effects.Should().Contain(e => e.EffectType == "ModifyDifficultyMultiplier");
        effectSet.Value.Effects.Should().Contain(e => e.EffectType == "ModifyRewardPowerMultiplier");
    }

    [Fact]
    public async Task EffectSetProvider_ShouldReturnCurseEffectSet()
    {
        var gateway = new InMemoryCatalogContentGateway();

        var effectSet = await gateway.GetEffectSetByKeyAsync("effectset.curse-old-wound");

        effectSet.IsSuccess.Should().BeTrue();
        effectSet.Value.Effects.Should().HaveCount(1);
        effectSet.Value.Effects.Single().EffectType.Should().Be("ModifyDifficultyMultiplier");
        effectSet.Value.Effects.Single().Duration.Should().Be("NextCombatOnly");
    }
}
