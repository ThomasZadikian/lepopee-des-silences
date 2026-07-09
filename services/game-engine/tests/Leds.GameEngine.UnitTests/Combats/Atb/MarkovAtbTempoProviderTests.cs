using FluentAssertions;
using Leds.GameEngine.Application.Combats.Atb;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Markov.Psyche;

namespace Leds.GameEngine.UnitTests.Combats.Atb;

public sealed class MarkovAtbTempoProviderTests
{
    private readonly MarkovAtbTempoProvider _provider = new();

    private static AtbTempoContext Ctx(EmotionalState dominant, CombatantSide side, int initiative = 0, string key = "c1", long tick = 0)
        => new(initiative, side, key, RunPsyche.Initial(dominant), "seed", tick);

    [Fact]
    public void Calm_player_gets_neutral_room_and_side_factors()
    {
        var result = _provider.Resolve(Ctx(EmotionalState.Calm, CombatantSide.Player));
        result.RoomFactorPerMille.Should().Be(1000);
        result.CombatantFactorPerMille.Should().Be(1050);
        result.OpeningGauge.Should().Be(0);
    }

    [Fact]
    public void Withdrawn_slows_the_hero()
    {
        var result = _provider.Resolve(Ctx(EmotionalState.Withdrawn, CombatantSide.Player));
        result.RoomFactorPerMille.Should().Be(900);
        result.CombatantFactorPerMille.Should().Be(950);
    }

    [Fact]
    public void Wary_speeds_enemies_and_gives_them_an_opening_bias()
    {
        var result = _provider.Resolve(Ctx(EmotionalState.Wary, CombatantSide.Enemy, initiative: 10));
        result.CombatantFactorPerMille.Should().Be(1100);
        result.OpeningGauge.Should().Be(3_000);
    }

    [Fact]
    public void Player_never_receives_an_opening_bias()
    {
        _provider.Resolve(Ctx(EmotionalState.Wary, CombatantSide.Player, initiative: 20)).OpeningGauge.Should().Be(2_000);
    }

    [Fact]
    public void Dissociated_jitters_room_factor_deterministically_and_within_bounds()
    {
        var a = _provider.Resolve(Ctx(EmotionalState.Dissociated, CombatantSide.Player, tick: 5));
        var b = _provider.Resolve(Ctx(EmotionalState.Dissociated, CombatantSide.Player, tick: 5));
        a.RoomFactorPerMille.Should().Be(b.RoomFactorPerMille);
        a.RoomFactorPerMille.Should().BeInRange(800, 1200);
    }

    [Fact]
    public void Fragmented_jitters_combatant_factor_deterministically_and_within_bounds()
    {
        var a = _provider.Resolve(Ctx(EmotionalState.Fragmented, CombatantSide.Enemy, key: "enemy.1", tick: 7));
        var b = _provider.Resolve(Ctx(EmotionalState.Fragmented, CombatantSide.Enemy, key: "enemy.1", tick: 7));
        a.CombatantFactorPerMille.Should().Be(b.CombatantFactorPerMille);
        a.CombatantFactorPerMille.Should().BeInRange(800, 1200);
    }
}
