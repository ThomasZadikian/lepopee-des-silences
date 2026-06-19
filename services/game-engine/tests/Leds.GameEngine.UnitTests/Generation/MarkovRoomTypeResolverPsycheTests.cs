using FluentAssertions;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Psyche;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;
using Leds.GameEngine.Infrastructure.Markov;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class MarkovRoomTypeResolverPsycheTests
{
    private const string Version = StaticRoomTypeMarkovMatrixProvider.SupportedVersion;

    // ⚠️ Ajuste les arguments du constructeur à TON câblage (trace sink si présent).
    private static MarkovRoomTypeResolver CreateResolver() =>
        new(
            new StaticRoomTypeMarkovMatrixProvider(),
            new MarkovTransitionResolver(new DeterministicMarkovSampler()),
            new NullMarkovTransitionTraceSink(),
            new EmotionalCalibration());

    private static int CountRupture(MarkovRoomTypeResolver resolver, RunPsyche psyche)
    {
        var count = 0;
        for (var s = 0; s < 3000; s++)
        {
            var type = resolver.ResolveNextRoomType($"seed-{s}", 5, RoomType.Memory, Version, psyche);
            if (type == RoomType.Rupture) count++;
        }
        return count;
    }

    [Fact]
    public void Fragmented_psyche_pulls_generation_toward_rupture()
    {
        var resolver = CreateResolver();

        var calm = CountRupture(resolver, RunPsyche.Initial(EmotionalState.Calm));
        var fragmented = CountRupture(resolver, RunPsyche.Initial(EmotionalState.Fragmented));

        // Fragmented préfère Rupture (.5) ; Calm ne l'attire pas (0). La causalité existe.
        fragmented.Should().BeGreaterThan(calm);
    }

    [Fact]
    public void Same_seed_and_psyche_is_deterministic()
    {
        var resolver = CreateResolver();
        var psyche = RunPsyche.Initial(EmotionalState.Fragmented);

        var a = resolver.ResolveNextRoomType("seed-x", 5, RoomType.Memory, Version, psyche);
        var b = resolver.ResolveNextRoomType("seed-x", 5, RoomType.Memory, Version, psyche);

        a.Should().Be(b);
    }
}