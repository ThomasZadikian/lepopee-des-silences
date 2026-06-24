using FluentAssertions;
using Leds.GameEngine.Application.Combats.Atb;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Markov.Psyche;

namespace Leds.GameEngine.UnitTests.Combats.Atb;

public sealed class MarkovAtbTempoProviderTests
{
    private readonly MarkovAtbTempoProvider _provider = new();

    private static AtbTempoContext Ctx(EmotionalState dominant, CombatantSide side, int speed = 100, int initiative = 0, string key = "c1", long tick = 0)
        => new(speed, initiative, side, key, RunPsyche.Initial(dominant), "seed", tick);

    [Fact]
    public void Calm_player_fills_with_room_and_side_factors()
    {
        var result = _provider.Resolve(Ctx(EmotionalState.Calm, CombatantSide.Player));
        result.FillPerTick.Should().Be(105);
        result.OpeningGauge.Should().Be(0);
    }

    [Fact]
    public void Withdrawn_slows_the_hero()
    {
        _provider.Resolve(Ctx(EmotionalState.Withdrawn, CombatantSide.Player)).FillPerTick.Should().Be(85);
    }

    [Fact]
    public void Wary_speeds_enemies_and_gives_them_an_opening_bias()
    {
        var result = _provider.Resolve(Ctx(EmotionalState.Wary, CombatantSide.Enemy, initiative: 10));
        result.FillPerTick.Should().Be(115);
        result.OpeningGauge.Should().Be(3_000);
    }

    [Fact]
    public void Player_never_receives_an_opening_bias()
    {
        _provider.Resolve(Ctx(EmotionalState.Wary, CombatantSide.Player, initiative: 20)).OpeningGauge.Should().Be(2_000);
    }

    [Fact]
    public void Dissociated_jitters_room_tempo_deterministically_and_within_bounds()
    {
        var a = _provider.Resolve(Ctx(EmotionalState.Dissociated, CombatantSide.Player, tick: 5));
        var b = _provider.Resolve(Ctx(EmotionalState.Dissociated, CombatantSide.Player, tick: 5));
        a.FillPerTick.Should().Be(b.FillPerTick);
        a.FillPerTick.Should().BeInRange(80, 120);
    }

    [Fact]
    public void Fill_never_drops_below_one()
    {
        _provider.Resolve(Ctx(EmotionalState.Withdrawn, CombatantSide.Player, speed: 1)).FillPerTick.Should().BeGreaterThanOrEqualTo(1);
    }
}