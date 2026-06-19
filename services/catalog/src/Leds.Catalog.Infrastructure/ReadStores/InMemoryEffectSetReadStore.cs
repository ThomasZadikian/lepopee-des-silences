using Leds.Catalog.Application.EffectSets.Dtos;
using Leds.Catalog.Application.EffectSets.Ports;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryEffectSetReadStore : IEffectSetReadStore
{
    private readonly IReadOnlyDictionary<string, EffectSetDto> _definitions =
        new Dictionary<string, EffectSetDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["effectset.curse-old-wound"] = EffectSet("effectset.curse-old-wound", Effect("ModifyDifficultyMultiplier", "NextCombat", 0.10m, "NextCombatOnly", "UniqueBySource")),
            ["effectset.curse-weight-of-silence"] = EffectSet("effectset.curse-weight-of-silence", Effect("ModifyDifficultyMultiplier", "NextCombat", 0.20m, "NextCombatOnly", "UniqueBySource")),
            ["effect.item.minor-heal"] = EffectSet("effect.item.minor-heal", Effect("HealVitality", "Self", 15m, "Immediate", "None")),
            ["effect.item.eclat-de-garde"] = EffectSet("effect.item.eclat-de-garde", Effect("AddStartingGuard", "NextCombat", 8m, "UntilRunEnds", "Additive"))
        };

    public Task<EffectSetDto?> GetDtoByKeyAsync(string key, CancellationToken cancellationToken)
    {
        _definitions.TryGetValue(key, out var definition);
        return Task.FromResult(definition);
    }

    private static EffectSetDto EffectSet(string key, params EffectDefinitionDto[] effects)
    {
        return new EffectSetDto(Guid.NewGuid(), key, "1.0.0", "Active", effects);
    }

    private static EffectDefinitionDto Effect(
        string effectType,
        string targetScope,
        decimal value,
        string duration,
        string stackPolicy)
    {
        return new EffectDefinitionDto(
            effectType,
            targetScope,
            value,
            "Flat",
            duration,
            stackPolicy,
            null,
            0,
            null,
            null,
            null);
    }
}
