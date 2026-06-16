using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Infrastructure.Catalog;

namespace Leds.GameEngine.UnitTests.Catalog;

public sealed class CatalogLawCurseProviderTests
{
    [Fact]
    public async Task PalaceLawProvider_ShouldReturnSeedLaw()
    {
        var provider = new InMemoryCatalogPalaceLawDefinitionProvider();

        var law = await provider.GetByKeyAsync("law-silence-v1");

        law.Should().NotBeNull();
        law!.Key.Should().Be("law-silence-v1");
        law.DisplayName.Should().Be("Loi du Silence");
        law.Duration.Should().Be("UntilRunEnds");
        law.EffectSetKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PalaceLawProvider_ShouldListAvailableLaws()
    {
        var provider = new InMemoryCatalogPalaceLawDefinitionProvider();

        var laws = await provider.ListAvailableAsync();

        laws.Should().NotBeEmpty();
        laws.Should().Contain(l => l.Key == "law-silence-v1");
    }

    [Fact]
    public async Task CurseProvider_ShouldReturnSeedCurse()
    {
        var provider = new InMemoryCatalogCurseDefinitionProvider();

        var curse = await provider.GetByKeyAsync("curse.old-wound");

        curse.Should().NotBeNull();
        curse!.Key.Should().Be("curse.old-wound");
        curse.DisplayName.Should().Be("Vieille blessure");
        curse.Duration.Should().Be("NextCombatOnly");
        curse.EffectSetKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CurseProvider_ShouldListAvailableCurses()
    {
        var provider = new InMemoryCatalogCurseDefinitionProvider();

        var curses = await provider.ListAvailableAsync();

        curses.Should().NotBeEmpty();
        curses.Should().Contain(c => c.Key == "curse.old-wound");
    }

    [Fact]
    public async Task EffectSetProvider_ShouldReturnLawEffectSet()
    {
        var provider = new InMemoryCatalogEffectSetProvider();

        var effectSet = await provider.GetEffectSetAsync("effectset.law-silence-v1");

        effectSet.Should().NotBeNull();
        effectSet!.Effects.Should().HaveCount(2);
        effectSet.Effects.Should().Contain(e => e.EffectType == "ModifyDifficultyMultiplier");
        effectSet.Effects.Should().Contain(e => e.EffectType == "ModifyRewardPowerMultiplier");
    }

    [Fact]
    public async Task EffectSetProvider_ShouldReturnCurseEffectSet()
    {
        var provider = new InMemoryCatalogEffectSetProvider();

        var effectSet = await provider.GetEffectSetAsync("effectset.curse-old-wound");

        effectSet.Should().NotBeNull();
        effectSet!.Effects.Should().HaveCount(1);
        effectSet.Effects.Single().EffectType.Should().Be("ModifyDifficultyMultiplier");
        effectSet.Effects.Single().Duration.Should().Be("NextCombatOnly");
    }
}
